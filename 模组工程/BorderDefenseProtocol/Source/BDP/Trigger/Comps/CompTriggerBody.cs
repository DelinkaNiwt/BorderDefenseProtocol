using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BDP.Core;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 触发体核心Comp——管理芯片槽位状态机和激活逻辑。
    /// 依赖BDP.Core.CompTrion（通过Pawn.GetComp获取）。
    ///
    /// ⚠️ 关键约束：装备后的武器CompTick()不会被调用。
    ///    Pawn_EquipmentTracker.EquipmentTrackerTick()只调用VerbsTick()，不调用CompTick()。
    ///    因此切换冷却等时间逻辑采用懒求值：在IsSwitching等属性访问时检查并结算。
    ///
    /// v2.0变更：
    ///   - T23：槽位语义从Main/Sub改为Left/Right
    ///   - T24：按侧Verb存储（leftHandActiveVerbProps/rightHandActiveVerbProps），由DualVerbCompositor合成
    ///   - §8.3：新增SetSideVerbs/ClearSideVerbs/GetChipSide API
    ///
    /// v5.0变更（6.2.1 Gizmo架构重设计）：
    ///   - IVerbOwner始终返回占位Verb + 芯片Verb（芯片Verb设hasStandardCommand=false）
    ///   - IVerbOwner.Tools始终返回parent.def.tools（移除近战抑制逻辑）
    ///   - 新增Verb引用缓存（leftHandAttackVerb/rightHandAttackVerb/dualAttackVerb）
    ///   - RebuildVerbs从WeaponChipEffect搬入，统一管理VerbTracker重建+缓存填充
    ///   - CompGetEquippedGizmosExtra通过Command_BDPChipAttack生成芯片攻击Gizmo
    ///
    /// v5.1变更（根因修复：芯片Verb脱离VerbTracker）：
    ///   - IVerbOwner.VerbProperties不再合并芯片Verb，只返回parent.def.Verbs
    ///   - 芯片Verb改为在RebuildVerbs中手动创建（Activator.CreateInstance + 反射设置verbTracker）
    ///   - 手动创建的Verb不在VerbTracker.AllVerbs中，彻底隔离于：
    ///     ① Pawn_MeleeVerbs近战选择池（IsMeleeAttack即入池，hasStandardCommand无效）
    ///     ② VerbTracker.GetVerbsCommands Path B（FirstOrDefault(IsMeleeAttack)绑定Y按钮）
    ///   - 芯片Verb只通过Command_BDPChipAttack gizmo使用
    ///
    /// 不变量：
    ///   ① 每侧激活芯片数 ≤ 1（左右手槽）；特殊槽无此限制（全部激活或全部关闭）
    ///   ② 已装载芯片数 ≤ 该侧槽位数
    ///   ③ hasRightHand==false时rightHandSlots为空
    ///   ④ switchState==Switching时pending!=null
    ///   ⑤ switchState==Idle时pending==null
    ///   ⑥ isActive==true的槽位loadedChip!=null
    ///   ⑦ allowChipManagement==false时loadedChip不可被玩家修改
    ///   ⑧ dualHandLockSlot!=null时，另一侧不可激活新芯片（v2.1）
    ///   ⑨ specialSlots全部同时激活/关闭，不参与switchState（v2.1）
    ///   ⑩ specialSlotCount==0时specialSlots为null（v2.1）
    ///   ⑪ 特殊槽芯片的激活/关闭必须全部同时进行，不可单独操作（v2.1.1）
    ///   ⑫ activationWarmup对特殊槽芯片无效（战斗体生成时立即激活）（v2.1.1）
    /// </summary>
    public class CompTriggerBody : CompEquippable, IVerbOwner
    {
        // ── 槽位数据（v2.0：mainSlots/subSlots → leftHandSlots/rightHandSlots） ──
        private List<ChipSlot> leftHandSlots;
        private List<ChipSlot> rightHandSlots;
        // v2.1（T29）：特殊槽，全部同时激活/关闭，不参与切换状态机
        private List<ChipSlot> specialSlots;

        // ── 切换状态机 ──
        private SwitchState switchState = SwitchState.Idle;
        private SwitchContext pending;

        // ── 调试用：临时覆盖冷却时间 ──
        private bool debugZeroCooldown = false;

        // v2.1（T31）：双手锁定槽位（非null=有双手芯片激活，另一侧被锁定）
        private ChipSlot dualHandLockSlot;

        // ── 按侧Verb存储（v2.0 T24：替代单一ActiveVerbProperties/ActiveTools） ──
        // 由WeaponChipEffect通过SetSideVerbs设置，DualVerbCompositor合成最终结果
        private List<VerbProperties> leftHandActiveVerbProps;
        private List<Tool> leftHandActiveTools;
        private List<VerbProperties> rightHandActiveVerbProps;
        private List<Tool> rightHandActiveTools;

        // ── v5.0新增：Verb引用缓存（不序列化，RebuildVerbs后重建） ──
        // 供CompGetEquippedGizmosExtra读取，生成Command_BDPChipAttack
        private Verb leftHandAttackVerb;    // 左手芯片独立攻击Verb实例
        private Verb rightHandAttackVerb;   // 右手芯片独立攻击Verb实例
        private Verb dualAttackVerb;        // 双手触发合成Verb实例

        /// <summary>
        /// 当前正在激活/关闭的槽位侧别（临时上下文）。
        /// 在DoActivate/DeactivateSlot中设置，供WeaponChipEffect等效果类读取自己所在侧。
        /// 调用effect.Activate/Deactivate前设置，调用后清除。
        /// </summary>
        internal SlotSide? ActivatingSide { get; private set; }

        /// <summary>
        /// 当前正在激活/关闭的槽位引用（临时上下文）。
        /// 在DoActivate/DeactivateSlot中与ActivatingSide同步设置/清除。
        /// 供Effect类通过此属性直接读取当前操作槽位的芯片DefModExtension。
        /// </summary>
        internal ChipSlot ActivatingSlot { get; private set; }

        /// <summary>
        /// 战斗体是否处于激活状态（由战斗模块控制）。
        /// true=已生成战斗体（芯片已Allocate），false=未生成。
        /// 影响UI四态显示：挂载未注册 vs 注册未激活。
        /// </summary>
        internal bool IsCombatBodyActive { get; private set; }

        // ── 显式实现IVerbOwner接口（v5.1改造：芯片Verb完全脱离VerbTracker） ──
        // VerbTracker.InitVerbs()通过IVerbOwner接口调用，显式实现可正确拦截
        //
        // v5.1改造（根因修复）：
        //   - VerbProperties只返回parent.def.Verbs（占位），不再合并芯片Verb
        //   - 原因：芯片Verb若在VerbTracker.AllVerbs中，会被引擎近战选择池
        //     （Pawn_MeleeVerbs.GetUpdatedAvailableVerbsList）和VerbTracker.GetVerbsCommands
        //     的近战武器路径（FirstOrDefault(IsMeleeAttack)）错误拾取
        //   - hasStandardCommand=false只控制Gizmo生成，不控制近战Verb选择
        //   - 芯片Verb改为在RebuildVerbs中手动创建，不进入VerbTracker.AllVerbs
        //   - Tools始终返回parent.def.tools（触发体"柄"的近战gizmo）

        List<VerbProperties> IVerbOwner.VerbProperties => parent.def.Verbs;

        List<Tool> IVerbOwner.Tools => parent.def.tools;

        // ── 便利属性 ──
        public CompProperties_TriggerBody Props => (CompProperties_TriggerBody)props;

        // Holder来自CompEquippable：(parent.ParentHolder as Pawn_EquipmentTracker)?.pawn
        private Pawn OwnerPawn => Holder;

        private CompTrion TrionComp => OwnerPawn?.GetComp<CompTrion>();

        // ── 公开属性（v2.0：MainSlots/SubSlots → LeftHandSlots/RightHandSlots） ──

        /// <summary>左手槽列表（只读，供UI层访问）。懒初始化以兼容CharacterEditor等外部工具。</summary>
        public IReadOnlyList<ChipSlot> LeftHandSlots { get { EnsureSlotsInitialized(); return leftHandSlots; } }
        /// <summary>右手槽列表（只读，供UI层访问）。懒初始化以兼容CharacterEditor等外部工具。</summary>
        public IReadOnlyList<ChipSlot> RightHandSlots { get { EnsureSlotsInitialized(); return rightHandSlots; } }
        /// <summary>特殊槽列表（只读，供UI层访问）。v2.1新增。</summary>
        public IReadOnlyList<ChipSlot> SpecialSlots { get { EnsureSlotsInitialized(); return specialSlots; } }

        /// <summary>
        /// 当前是否处于切换空窗期。
        /// 懒求值：访问时自动结算到期的冷却。
        /// </summary>
        public bool IsSwitching
        {
            get
            {
                TryResolvePendingSwitch();
                return switchState == SwitchState.Switching;
            }
        }

        /// <summary>切换进度（0=刚开始，1=完成），仅IsSwitching时有意义。</summary>
        public float SwitchProgress
        {
            get
            {
                TryResolvePendingSwitch();
                if (pending == null || Props.switchCooldownTicks <= 0) return 1f;
                int remaining = pending.cooldownTick - Find.TickManager.TicksGame;
                return 1f - Mathf.Clamp01((float)remaining / Props.switchCooldownTicks);
            }
        }

        /// <summary>懒求值：检查切换冷却是否到期，到期则结算。</summary>
        private void TryResolvePendingSwitch()
        {
            if (switchState != SwitchState.Switching || pending == null) return;
            if (Find.TickManager.TicksGame < pending.cooldownTick) return;

            // 冷却到期，尝试激活目标芯片
            if (CanActivateChip(pending.side, pending.slotIndex))
                DoActivate(GetSlot(pending.side, pending.slotIndex));

            switchState = SwitchState.Idle;
            pending = null;
        }

        /// <summary>懒初始化槽位列表（兼容CharacterEditor等外部工具）。</summary>
        private void EnsureSlotsInitialized()
        {
            if (leftHandSlots == null)
                leftHandSlots = InitSlots(SlotSide.LeftHand, Props.leftHandSlotCount);
            if (Props.hasRightHand && rightHandSlots == null)
                rightHandSlots = InitSlots(SlotSide.RightHand, Props.rightHandSlotCount);
            // v2.1：特殊槽懒初始化
            if (Props.specialSlotCount > 0 && specialSlots == null)
                specialSlots = InitSlots(SlotSide.Special, Props.specialSlotCount);
        }

        /// <summary>检查装备者Pawn是否拥有Trion腺体基因。</summary>
        public bool OwnerHasTrionGland()
        {
            var pawn = OwnerPawn;
            return pawn?.genes?.HasActiveGene(BDP_DefOf.BDP_Gene_TrionGland) ?? false;
        }

        // ── 槽位访问 ──

        public ChipSlot GetSlot(SlotSide side, int index)
        {
            // v2.1：支持Special侧
            var list = side == SlotSide.LeftHand ? leftHandSlots
                     : side == SlotSide.RightHand ? rightHandSlots
                     : side == SlotSide.Special ? specialSlots
                     : null;
            if (list == null || index < 0 || index >= list.Count) return null;
            return list[index];
        }

        public IEnumerable<ChipSlot> AllSlots()
        {
            if (leftHandSlots != null) foreach (var s in leftHandSlots) yield return s;
            if (rightHandSlots != null) foreach (var s in rightHandSlots) yield return s;
            // v2.1：包含特殊槽
            if (specialSlots != null) foreach (var s in specialSlots) yield return s;
        }

        public IEnumerable<ChipSlot> AllActiveSlots()
            => AllSlots().Where(s => s.isActive && s.loadedChip != null);

        public bool HasAnyActiveChip()
            => AllSlots().Any(s => s.isActive);

        /// <summary>指定侧是否有激活芯片（§6.3接口约定，供战斗模块检查）。</summary>
        public bool HasActiveChip(SlotSide side)
            => GetActiveSlot(side) != null;

        public ChipSlot GetActiveSlot(SlotSide side)
        {
            // v2.1：支持Special侧
            var list = side == SlotSide.LeftHand ? leftHandSlots
                     : side == SlotSide.RightHand ? rightHandSlots
                     : side == SlotSide.Special ? specialSlots
                     : null;
            return list?.FirstOrDefault(s => s.isActive);
        }

        // ═══════════════════════════════════════════
        //  按侧Verb管理（v2.0 §8.3 新增API）
        // ═══════════════════════════════════════════

        /// <summary>设置指定侧的Verb/Tool数据（供WeaponChipEffect调用）。</summary>
        public void SetSideVerbs(SlotSide side, List<VerbProperties> verbs, List<Tool> tools)
        {
            if (side == SlotSide.LeftHand)
            {
                leftHandActiveVerbProps = verbs;
                leftHandActiveTools = tools;
            }
            else
            {
                rightHandActiveVerbProps = verbs;
                rightHandActiveTools = tools;
            }
        }

        /// <summary>清除指定侧的Verb/Tool数据（供WeaponChipEffect调用）。</summary>
        public void ClearSideVerbs(SlotSide side)
        {
            if (side == SlotSide.LeftHand)
            {
                leftHandActiveVerbProps = null;
                leftHandActiveTools = null;
            }
            else
            {
                rightHandActiveVerbProps = null;
                rightHandActiveTools = null;
            }
        }

        /// <summary>查找芯片所在侧别（遍历所有激活槽位）。</summary>
        public SlotSide GetChipSide(Thing chip)
        {
            foreach (var slot in AllActiveSlots())
                if (slot.loadedChip == chip) return slot.side;
            // 未找到时默认左手槽
            return SlotSide.LeftHand;
        }

        // ═══════════════════════════════════════════
        //  VerbTracker重建 + 芯片Verb手动创建（v5.1根因修复）
        // ═══════════════════════════════════════════

        // 反射缓存：Verb.verbTracker是internal字段，需要反射设置
        // 原因：手动创建的Verb需要verbTracker才能正确获取EquipmentSource
        private static readonly FieldInfo fi_verbTracker =
            typeof(Verb).GetField("verbTracker", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        // Bug7修复：静态构造函数中断言反射字段存在，避免静默失败
        static CompTriggerBody()
        {
            if (fi_verbTracker == null)
                Log.Error("[BDP] 致命：无法找到Verb.verbTracker字段，芯片攻击系统将无法工作。" +
                          "可能是RimWorld版本更新导致字段名变更，请检查Verse.Verb的内部字段。");
        }

        /// <summary>
        /// 重建触发体VerbTracker、手动创建芯片Verb实例、填充缓存（v5.1）。
        ///
        /// v5.1根因修复：芯片Verb不再通过IVerbOwner.VerbProperties进入VerbTracker。
        /// 原因：VerbTracker.AllVerbs中的近战Verb会被引擎的两条路径错误拾取：
        ///   ① Pawn_MeleeVerbs.GetUpdatedAvailableVerbsList — 近战选择池（IsMeleeAttack即入池）
        ///   ② VerbTracker.GetVerbsCommands Path B — FirstOrDefault(IsMeleeAttack)绑定Y按钮
        /// hasStandardCommand=false只控制Path A（标准Gizmo生成），对①②无效。
        ///
        /// 新流程：
        ///   1. InitVerbsFromZero() — 只重建占位Verb（来自parent.def.Verbs + Tools）
        ///   2. 绑定caster
        ///   3. DualVerbCompositor.ComposeVerbs() — 合成芯片VerbProperties
        ///   4. 手动Activator.CreateInstance创建芯片Verb实例（不进入AllVerbs）
        ///   5. 通过反射设置verb.verbTracker（使EquipmentSource正确指向触发体）
        ///   6. 缓存到leftHandAttackVerb/rightHandAttackVerb/dualAttackVerb
        /// </summary>
        public void RebuildVerbs(Pawn pawn)
        {
            if (VerbTracker == null) return;

            // 步骤1：重建VerbTracker（只包含占位Verb + Tool产生的"柄"Verb）
            VerbTracker.InitVerbsFromZero();

            // 步骤2：重新绑定caster（模拟Pawn_EquipmentTracker.Notify_EquipmentAdded的行为）
            if (pawn != null && AllVerbs != null)
            {
                foreach (var verb in AllVerbs)
                    verb.caster = pawn;
            }

            // 步骤3+4+5+6：合成芯片VerbProperties → 手动创建Verb实例 → 缓存
            CreateAndCacheChipVerbs(pawn);

            // 诊断日志
            if (Prefs.DevMode)
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"[BDP] RebuildVerbs完成 [{parent.LabelShortCap}] pawn={pawn?.LabelShortCap}");
                if (AllVerbs != null)
                {
                    sb.AppendLine($"  VerbTracker.AllVerbs ({AllVerbs.Count}) — 芯片Verb不在此列表中:");
                    for (int i = 0; i < AllVerbs.Count; i++)
                    {
                        var v = AllVerbs[i];
                        sb.AppendLine($"    [{i}] {v.GetType().Name} primary={v.verbProps?.isPrimary} stdCmd={v.verbProps?.hasStandardCommand} melee={v.IsMeleeAttack} label={v.verbProps?.label} caster={v.caster?.LabelShortCap}");
                    }
                }
                sb.AppendLine($"  芯片Verb缓存（手动创建，不在AllVerbs中）:");
                sb.AppendLine($"    left={leftHandAttackVerb?.GetType().Name} right={rightHandAttackVerb?.GetType().Name} dual={dualAttackVerb?.GetType().Name}");
                Log.Message(sb.ToString());
            }
        }

        /// <summary>
        /// 合成芯片VerbProperties，手动创建Verb实例，缓存引用（v5.1）。
        ///
        /// 手动创建的Verb实例不在VerbTracker.AllVerbs中，因此：
        ///   · 不会被Pawn_MeleeVerbs近战选择池拾取
        ///   · 不会被VerbTracker.GetVerbsCommands的Path B拾取
        ///   · 只通过Command_BDPChipAttack gizmo使用
        /// </summary>
        private void CreateAndCacheChipVerbs(Pawn pawn)
        {
            leftHandAttackVerb = null;
            rightHandAttackVerb = null;
            dualAttackVerb = null;

            // 合成芯片VerbProperties
            var chipVerbProps = DualVerbCompositor.ComposeVerbs(
                leftHandActiveVerbProps, rightHandActiveVerbProps,
                GetActiveSlot(SlotSide.LeftHand), GetActiveSlot(SlotSide.RightHand));

            if (chipVerbProps == null) return;

            // 为每个芯片VerbProperties手动创建Verb实例
            foreach (var vp in chipVerbProps)
            {
                if (vp.verbClass == null) continue;

                Verb verb;
                try
                {
                    verb = (Verb)System.Activator.CreateInstance(vp.verbClass);
                }
                catch (System.Exception ex)
                {
                    Log.Error($"[BDP] 创建芯片Verb失败: {vp.verbClass.Name} — {ex}");
                    continue;
                }

                // 模拟VerbTracker.InitVerb的字段设置
                verb.loadID = $"BDP_Chip_{parent.ThingID}_{chipVerbProps.IndexOf(vp)}";
                verb.verbProps = vp;
                verb.caster = pawn;
                // verb.tool = null（芯片Verb不基于Tool）
                // verb.maneuver = null

                // 反射设置verbTracker，使verb.EquipmentSource正确指向触发体
                // （EquipmentSource = (DirectOwner as CompEquippable)?.parent）
                fi_verbTracker?.SetValue(verb, VerbTracker);

                // 按type+label分配到缓存槽位
                var vType = verb.GetType();
                var label = vp.label;

                if (vType == typeof(Verb_BDPMelee) || vType == typeof(Verb_BDPShoot))
                {
                    var side = DualVerbCompositor.ParseSideLabel(label);
                    if (side == SlotSide.LeftHand)
                        leftHandAttackVerb = verb;
                    else if (side == SlotSide.RightHand)
                        rightHandAttackVerb = verb;
                    else
                        dualAttackVerb = verb;
                }
                else if (vType == typeof(Verb_BDPDualRanged))
                {
                    dualAttackVerb = verb;
                }
            }
        }

        // ═══════════════════════════════════════════
        //  芯片装载/卸载
        // ═══════════════════════════════════════════

        /// <summary>将芯片物品装入指定槽位（芯片被触发体"吸收"，从持有者库存移除）。</summary>
        public bool LoadChip(SlotSide side, int slotIndex, Thing chip)
        {
            if (!Props.allowChipManagement) return false;
            var slot = GetSlot(side, slotIndex);
            if (slot == null || slot.loadedChip != null) return false;
            if (chip?.TryGetComp<TriggerChipComp>() == null) return false;

            slot.loadedChip = chip;
            chip.holdingOwner?.Remove(chip);
            return true;
        }

        /// <summary>从槽位取出芯片物品（生成到Pawn库存或地面）。</summary>
        public bool UnloadChip(SlotSide side, int slotIndex)
        {
            if (!Props.allowChipManagement) return false;
            var slot = GetSlot(side, slotIndex);
            if (slot?.loadedChip == null) return false;

            if (slot.isActive) DeactivateSlot(slot);

            var chip = slot.loadedChip;
            slot.loadedChip = null;

            var pawn = OwnerPawn;
            if (pawn != null && pawn.inventory != null)
                pawn.inventory.TryAddItemNotForSale(chip);
            else
                GenPlace.TryPlaceThing(chip, parent.Position, parent.Map, ThingPlaceMode.Near);

            return true;
        }

        // ═══════════════════════════════════════════
        //  前置条件检查
        // ═══════════════════════════════════════════

        /// <summary>
        /// 检查指定槽位是否可以激活（供UI灰显和ActivateChip前置检查）。
        /// v2.1新增检查：minOutputPower、dualHandLock、exclusionTags。
        /// </summary>
        public bool CanActivateChip(SlotSide side, int slotIndex)
        {
            var slot = GetSlot(side, slotIndex);
            if (slot?.loadedChip == null) return false;

            var chipComp = slot.loadedChip.TryGetComp<TriggerChipComp>();
            if (chipComp == null) return false;

            var pawn = OwnerPawn;
            if (pawn == null) return false;

            var trion = TrionComp;

            // ── 检查1：激活成本 ──
            if (trion != null && chipComp.Props.activationCost > 0f)
                if (trion.Available < chipComp.Props.activationCost) return false;

            // ── 检查2（v2.1）：最低输出功率 ──
            if (chipComp.Props.minOutputPower > 0f)
                if (pawn.GetStatValue(BDP_DefOf.BDP_TrionOutputPower) < chipComp.Props.minOutputPower)
                    return false;

            // ── 检查3（v2.1）：双手锁定 ──
            // 若有双手芯片激活且不是本槽位，则拒绝
            if (dualHandLockSlot != null && dualHandLockSlot != slot)
                return false;

            // ── 检查4（v2.1）：互斥标签（对称检查） ──
            // exclusionTags同时作为"我的标签"和"我排斥的标签"，交集非空则拒绝
            // 对称性：A∩B = B∩A，只需单向检查
            var myExclusions = chipComp.Props.exclusionTags;
            if (myExclusions != null && myExclusions.Count > 0)
            {
                foreach (var activeSlot in AllActiveSlots())
                {
                    if (activeSlot == slot) continue;
                    var activeExclusions = activeSlot.loadedChip?.TryGetComp<TriggerChipComp>()?.Props.exclusionTags;
                    if (activeExclusions == null) continue;
                    foreach (var tag in myExclusions)
                        if (activeExclusions.Contains(tag)) return false;
                }
            }

            // ── 检查5：效果自身的CanActivate ──
            var effect = chipComp.GetEffect();
            return effect?.CanActivate(pawn, parent) ?? false;
        }

        // ═══════════════════════════════════════════
        //  激活/关闭
        // ═══════════════════════════════════════════

        /// <summary>激活指定槽位（含切换逻辑）。</summary>
        public bool ActivateChip(SlotSide side, int slotIndex)
        {
            var slot = GetSlot(side, slotIndex);
            if (slot?.loadedChip == null) return false;
            if (!CanActivateChip(side, slotIndex)) return false;
            if (switchState == SwitchState.Switching) return false;

            // v2.1：Special侧不走切换逻辑，委托给ActivateAllSpecial
            if (side == SlotSide.Special)
            {
                ActivateAllSpecial();
                return true;
            }

            var existingActive = GetActiveSlot(side);
            var chipComp = slot.loadedChip.TryGetComp<TriggerChipComp>();

            // v2.1（T31）：双手芯片——先关闭对侧，然后走直接激活路径（不经过Switching）
            if (chipComp?.Props.isDualHand == true)
            {
                var oppositeSide = side == SlotSide.LeftHand ? SlotSide.RightHand : SlotSide.LeftHand;
                var oppositeActive = GetActiveSlot(oppositeSide);
                if (oppositeActive != null)
                    DeactivateSlot(oppositeActive);
                // 同侧有旧芯片也先关闭
                if (existingActive != null && existingActive != slot)
                    DeactivateSlot(existingActive);
                DoActivate(slot);
                return true;
            }

            // v2.1（T33）：切换冷却 = max(switchCooldown, activationWarmup)
            int warmup = chipComp?.Props.activationWarmup ?? 0;
            int cooldown = debugZeroCooldown ? 0
                         : System.Math.Max(Props.switchCooldownTicks, warmup);

            if (existingActive != null && existingActive != slot)
            {
                // 切换路径：先关闭旧芯片，进入空窗期
                DeactivateSlot(existingActive);
                switchState = SwitchState.Switching;
                pending = new SwitchContext(side, slotIndex, Find.TickManager.TicksGame + cooldown);
            }
            else
            {
                // 直接激活路径：若有warmup也走空窗期（无旧芯片需关闭）
                if (warmup > 0 && !debugZeroCooldown)
                {
                    switchState = SwitchState.Switching;
                    pending = new SwitchContext(side, slotIndex, Find.TickManager.TicksGame + warmup);
                }
                else
                {
                    DoActivate(slot);
                }
            }

            return true;
        }

        /// <summary>关闭指定侧的当前激活芯片。</summary>
        public void DeactivateChip(SlotSide side)
        {
            var active = GetActiveSlot(side);
            if (active != null) DeactivateSlot(active);
        }

        /// <summary>关闭所有激活芯片（卸下触发体时调用）。</summary>
        /// <param name="pawn">显式传入的Pawn引用（卸下装备时OwnerPawn可能已为null）。</param>
        public void DeactivateAll(Pawn pawn = null)
        {
            foreach (var slot in AllSlots())
            {
                if (!slot.isActive) continue;
                try
                {
                    DeactivateSlot(slot, pawn);
                }
                catch (System.Exception ex)
                {
                    // 单个slot失败不影响其他slot的关闭
                    Log.Error($"[BDP] DeactivateSlot异常 ({slot}): {ex}");
                    slot.isActive = false; // 强制标记为关闭，防止残留
                }
            }

            // 清除按侧Verb数据（v2.0）
            leftHandActiveVerbProps = null; leftHandActiveTools = null;
            rightHandActiveVerbProps = null; rightHandActiveTools = null;

            switchState = SwitchState.Idle;
            pending = null;
            // v2.1：清除双手锁定
            dualHandLockSlot = null;
        }

        /// <summary>
        /// 激活所有特殊槽（全部同时激活）。
        /// 特殊槽不参与switchState，不受切换冷却影响（不变量⑨⑫）。
        /// 由战斗体模块在战斗体生成时调用。v2.1新增。
        /// </summary>
        public void ActivateAllSpecial()
        {
            if (specialSlots == null) return;
            foreach (var slot in specialSlots)
            {
                if (slot.loadedChip == null || slot.isActive) continue;
                if (CanActivateChip(SlotSide.Special, slot.index))
                    DoActivate(slot);
            }
        }

        /// <summary>
        /// 关闭所有特殊槽（全部同时关闭）。
        /// 由战斗体模块在战斗体解除时调用。v2.1新增。
        /// </summary>
        public void DeactivateAllSpecial()
        {
            if (specialSlots == null) return;
            foreach (var slot in specialSlots)
                if (slot.isActive) DeactivateSlot(slot, null);
        }

        private void DoActivate(ChipSlot slot)
        {
            var pawn = OwnerPawn;
            if (pawn == null) return;

            var chipComp = slot.loadedChip.TryGetComp<TriggerChipComp>();
            var effect = chipComp?.GetEffect();
            if (effect == null) return;

            // ── 一次性激活成本 ──
            float cost = chipComp.Props.activationCost;
            if (cost > 0f) TrionComp?.Consume(cost);

            // ── v2.1（T32）：统一注册持续消耗 ──
            if (chipComp.Props.drainPerDay > 0f)
                TrionComp?.RegisterDrain($"chip_{slot.side}_{slot.index}", chipComp.Props.drainPerDay);

            // 设置激活上下文（供WeaponChipEffect等读取侧别和槽位）
            ActivatingSide = slot.side;
            ActivatingSlot = slot;
            effect.Activate(pawn, parent);
            ActivatingSide = null;
            ActivatingSlot = null;

            slot.isActive = true;

            // ── v2.1（T31）：双手锁定 ──
            if (chipComp.Props.isDualHand)
                dualHandLockSlot = slot;

            // ── v4.0（F1）：组合能力查询 ──
            TryGrantComboAbility(pawn);
        }

        /// <param name="pawnOverride">显式Pawn引用，优先于OwnerPawn（卸下装备时OwnerPawn为null）。</param>
        private void DeactivateSlot(ChipSlot slot, Pawn pawnOverride = null)
        {
            if (!slot.isActive || slot.loadedChip == null) return;
            var pawn = pawnOverride ?? OwnerPawn;
            var chipComp = slot.loadedChip.TryGetComp<TriggerChipComp>();
            var effect = chipComp?.GetEffect();

            // ── v2.1（T32）：统一注销持续消耗 ──
            TrionComp?.UnregisterDrain($"chip_{slot.side}_{slot.index}");

            // 设置激活上下文（供WeaponChipEffect等读取侧别和槽位）
            ActivatingSide = slot.side;
            ActivatingSlot = slot;
            effect?.Deactivate(pawn, parent);
            ActivatingSide = null;
            ActivatingSlot = null;

            slot.isActive = false;

            // ── v2.1（T31）：清除双手锁定 ──
            if (dualHandLockSlot == slot)
                dualHandLockSlot = null;

            // ── v4.0（F1）：组合能力移除（芯片关闭后重新检查） ──
            TryRevokeComboAbilities(pawn);
        }

        // ═══════════════════════════════════════════
        //  CompTick — 已移除
        //  原因：装备后的武器CompTick()不被调用。
        //  切换冷却改为懒求值，由UI每帧访问IsSwitching时触发。
        // ═══════════════════════════════════════════

        // ═══════════════════════════════════════════
        //  生命周期
        // ═══════════════════════════════════════════

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            // B5修复：只在槽位尚未初始化时才初始化。
            // 原因：触发体从装备卸下掉到地面时，GenSpawn.Spawn调用PostSpawnSetup(respawningAfterLoad: false)，
            // 若无条件重新初始化会清空已装载的芯片。加 leftHandSlots == null 守卫避免覆盖。
            if (!respawningAfterLoad && leftHandSlots == null)
            {
                leftHandSlots = InitSlots(SlotSide.LeftHand, Props.leftHandSlotCount);
                rightHandSlots = Props.hasRightHand ? InitSlots(SlotSide.RightHand, Props.rightHandSlotCount) : null;
                // v2.1：初始化特殊槽
                specialSlots = Props.specialSlotCount > 0
                             ? InitSlots(SlotSide.Special, Props.specialSlotCount)
                             : null;

                if (Props.preloadedChips != null)
                {
                    foreach (var cfg in Props.preloadedChips)
                    {
                        if (cfg.chipDef == null) continue;
                        var chip = ThingMaker.MakeThing(cfg.chipDef);
                        LoadChipInternal(cfg.side, cfg.slotIndex, chip);
                    }
                }
            }
        }

        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);
            EnsureSlotsInitialized();

            if (Props.autoActivateOnEquip)
            {
                foreach (var slot in AllSlots())
                    if (slot.loadedChip != null && !slot.isActive)
                        ActivateChip(slot.side, slot.index);
            }
        }

        public override void Notify_Unequipped(Pawn pawn)
        {
            DeactivateAll(pawn);
            base.Notify_Unequipped(pawn);
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            DeactivateAll();
            base.PostDestroy(mode, previousMap);
        }

        private static List<ChipSlot> InitSlots(SlotSide side, int count)
        {
            var list = new List<ChipSlot>(count);
            for (int i = 0; i < count; i++) list.Add(new ChipSlot(i, side));
            return list;
        }

        /// <summary>内部装载（不检查allowChipManagement，用于预装和读档恢复）。</summary>
        private void LoadChipInternal(SlotSide side, int slotIndex, Thing chip)
        {
            var slot = GetSlot(side, slotIndex);
            if (slot == null || slot.loadedChip != null) return;
            slot.loadedChip = chip;
        }

        // ═══════════════════════════════════════════
        //  存档
        // ═══════════════════════════════════════════

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Collections.Look(ref leftHandSlots, "leftHandSlots", LookMode.Deep);
            Scribe_Collections.Look(ref rightHandSlots, "rightHandSlots", LookMode.Deep);
            // v2.1：序列化特殊槽
            Scribe_Collections.Look(ref specialSlots, "specialSlots", LookMode.Deep);
            Scribe_Values.Look(ref switchState, "switchState");
            Scribe_Deep.Look(ref pending, "pending");
            // dualHandLockSlot是运行时引用，不序列化，读档后由激活恢复逻辑重建

            // v3.1：序列化IsCombatBodyActive（战斗体状态需跨存档保持）
            bool isCombatBodyActive = IsCombatBodyActive;
            Scribe_Values.Look(ref isCombatBodyActive, "isCombatBodyActive");
            IsCombatBodyActive = isCombatBodyActive;

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (leftHandSlots == null) leftHandSlots = InitSlots(SlotSide.LeftHand, Props.leftHandSlotCount);
                if (Props.hasRightHand && rightHandSlots == null) rightHandSlots = InitSlots(SlotSide.RightHand, Props.rightHandSlotCount);
                // v2.1：特殊槽读档恢复
                if (Props.specialSlotCount > 0 && specialSlots == null)
                    specialSlots = InitSlots(SlotSide.Special, Props.specialSlotCount);

                if (switchState == SwitchState.Idle) pending = null;

                // 恢复激活效果（IChipEffect无状态，重新调用Activate()）
                var pawn = OwnerPawn;
                if (pawn != null)
                {
                    foreach (var slot in AllSlots())
                    {
                        if (slot.isActive && slot.loadedChip != null)
                        {
                            var chipComp = slot.loadedChip.TryGetComp<TriggerChipComp>();
                            var effect = chipComp?.GetEffect();
                            ActivatingSide = slot.side;
                            ActivatingSlot = slot;
                            effect?.Activate(pawn, parent);
                            ActivatingSide = null;
                            ActivatingSlot = null;

                            // v2.1：读档后重建dualHandLockSlot
                            if (chipComp?.Props.isDualHand == true)
                                dualHandLockSlot = slot;
                        }
                    }
                }
            }
        }

        // ═══════════════════════════════════════════
        //  Gizmo
        // ═══════════════════════════════════════════

        public override IEnumerable<Gizmo> CompGetEquippedGizmosExtra()
        {
            foreach (var g in base.CompGetEquippedGizmosExtra()) yield return g;

            if (!OwnerHasTrionGland()) yield break;

            // ── v5.0新增：芯片攻击Gizmo（通过Command_BDPChipAttack自定义生成） ──
            var leftSlot = GetActiveSlot(SlotSide.LeftHand);
            var rightSlot = GetActiveSlot(SlotSide.RightHand);
            var leftChipDef = leftSlot?.loadedChip?.def;
            var rightChipDef = rightSlot?.loadedChip?.def;

            if (leftHandAttackVerb != null && leftChipDef != null)
            {
                yield return new Command_BDPChipAttack
                {
                    verb = leftHandAttackVerb,
                    attackId = leftChipDef.defName,
                    icon = leftChipDef.uiIcon,
                    defaultLabel = leftChipDef.label,
                };
            }
            if (rightHandAttackVerb != null && rightChipDef != null)
            {
                yield return new Command_BDPChipAttack
                {
                    verb = rightHandAttackVerb,
                    attackId = rightChipDef.defName,
                    icon = rightChipDef.uiIcon,
                    defaultLabel = rightChipDef.label,
                };
            }
            if (dualAttackVerb != null && leftChipDef != null && rightChipDef != null)
            {
                // 排序保证A+B=B+A
                var a = leftChipDef.defName;
                var b = rightChipDef.defName;
                if (string.Compare(a, b, System.StringComparison.Ordinal) > 0)
                { var tmp = a; a = b; b = tmp; }

                yield return new Command_BDPChipAttack
                {
                    verb = dualAttackVerb,
                    attackId = "dual:" + a + "+" + b,
                    icon = parent.def.uiIcon, // 触发体图标
                    defaultLabel = "双手触发",
                };
            }

            // v2.1.1：allowChipManagement=false时不显示状态Gizmo
            // 原因：近界/黑触发器玩家无法操作芯片，显示Gizmo无意义
            if (Props.allowChipManagement)
                yield return new Gizmo_TriggerBodyStatus(this);

            if (!DebugSettings.godMode) yield break;
            foreach (var g in GetDebugGizmos()) yield return g;
        }

        // ═══════════════════════════════════════════
        //  组合能力系统（v4.0 F1新增）
        // ═══════════════════════════════════════════

        // 当前已授予的组合能力（运行时跟踪，不序列化——读档时由激活恢复逻辑重建）
        private readonly List<ComboAbilityDef> grantedCombos = new List<ComboAbilityDef>();

        /// <summary>
        /// 芯片激活后检查是否满足组合条件，满足则授予Ability。
        /// 遍历DefDatabase&lt;ComboAbilityDef&gt;，匹配当前左右手激活芯片。
        /// </summary>
        private void TryGrantComboAbility(Pawn pawn)
        {
            if (pawn?.abilities == null) return;
            var leftSlot = GetActiveSlot(SlotSide.LeftHand);
            var rightSlot = GetActiveSlot(SlotSide.RightHand);
            if (leftSlot?.loadedChip == null || rightSlot?.loadedChip == null) return;

            foreach (var combo in DefDatabase<ComboAbilityDef>.AllDefs)
            {
                if (grantedCombos.Contains(combo)) continue;
                if (!combo.Matches(leftSlot.loadedChip.def, rightSlot.loadedChip.def)) continue;
                if (combo.abilityDef == null) continue;

                pawn.abilities.GainAbility(combo.abilityDef);
                grantedCombos.Add(combo);
            }
        }

        /// <summary>
        /// 芯片关闭后检查已授予的组合能力是否仍然满足条件，不满足则移除。
        /// </summary>
        private void TryRevokeComboAbilities(Pawn pawn)
        {
            if (pawn?.abilities == null || grantedCombos.Count == 0) return;
            var leftSlot = GetActiveSlot(SlotSide.LeftHand);
            var rightSlot = GetActiveSlot(SlotSide.RightHand);

            for (int i = grantedCombos.Count - 1; i >= 0; i--)
            {
                var combo = grantedCombos[i];
                bool stillValid = leftSlot?.loadedChip != null && rightSlot?.loadedChip != null
                    && combo.Matches(leftSlot.loadedChip.def, rightSlot.loadedChip.def);
                if (!stillValid)
                {
                    if (combo.abilityDef != null)
                        pawn.abilities.RemoveAbility(combo.abilityDef);
                    grantedCombos.RemoveAt(i);
                }
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var g in base.CompGetGizmosExtra()) yield return g;
        }

        private IEnumerable<Gizmo> GetDebugGizmos()
        {
            bool hasEmptyLeft = HasEmptySlot(SlotSide.LeftHand);
            yield return new Command_ActionWithMenu
            {
                defaultLabel = "[Dev] 填充左手槽",
                defaultDesc = "左键：随机填充\n右键：选择芯片",
                action = () => FillRandomChip(SlotSide.LeftHand),
                menuOptions = GetChipMenuOptions(SlotSide.LeftHand),
                Disabled = !hasEmptyLeft,
                disabledReason = "左手槽无空槽"
            };
            bool hasEmptyRight = Props.hasRightHand && HasEmptySlot(SlotSide.RightHand);
            yield return new Command_ActionWithMenu
            {
                defaultLabel = "[Dev] 填充右手槽",
                defaultDesc = "左键：随机填充\n右键：选择芯片",
                action = () => FillRandomChip(SlotSide.RightHand),
                menuOptions = GetChipMenuOptions(SlotSide.RightHand),
                Disabled = !hasEmptyRight,
                disabledReason = "右手槽无空槽或无右手槽"
            };
            yield return new Command_Action
            {
                defaultLabel = "[Dev] 清空左手槽",
                defaultDesc = "关闭并卸载左手槽所有芯片（绕过allowChipManagement）",
                action = () => ClearSide(SlotSide.LeftHand)
            };
            yield return new Command_Action
            {
                defaultLabel = "[Dev] 清空右手槽",
                defaultDesc = "关闭并卸载右手槽所有芯片（绕过allowChipManagement）",
                action = () => ClearSide(SlotSide.RightHand)
            };
            // v2.1：特殊槽调试按钮
            if (Props.specialSlotCount > 0)
            {
                bool hasEmptySpecial = HasEmptySlot(SlotSide.Special);
                yield return new Command_ActionWithMenu
                {
                    defaultLabel = "[Dev] 填充特殊槽",
                    defaultDesc = "左键：随机填充\n右键：选择芯片",
                    action = () => FillRandomChip(SlotSide.Special),
                    menuOptions = GetChipMenuOptions(SlotSide.Special),
                    Disabled = !hasEmptySpecial,
                    disabledReason = "特殊槽无空槽"
                };
                yield return new Command_Action
                {
                    defaultLabel = "[Dev] 清空特殊槽",
                    defaultDesc = "关闭并卸载特殊槽所有芯片（绕过allowChipManagement）",
                    action = () => ClearSide(SlotSide.Special)
                };
                yield return new Command_Action
                {
                    defaultLabel = "[Dev] 激活全部特殊槽",
                    defaultDesc = "调用ActivateAllSpecial()",
                    action = ActivateAllSpecial
                };
                yield return new Command_Action
                {
                    defaultLabel = "[Dev] 关闭全部特殊槽",
                    defaultDesc = "调用DeactivateAllSpecial()",
                    action = DeactivateAllSpecial
                };
            }
            yield return new Command_Action
            {
                defaultLabel = "[Dev] 状态转储",
                defaultDesc = "Log输出所有槽位状态、激活状态、Trion数据",
                action = DumpState
            };
            yield return new Command_Action
            {
                defaultLabel = "[Dev] 强制关闭",
                defaultDesc = "立即关闭所有激活效果（跳过空窗期）",
                action = () => DeactivateAll()
            };
            yield return new Command_Action
            {
                defaultLabel = "[Dev] 重建Verb",
                defaultDesc = "强制调用pawn.verbTracker.InitVerbsFromZero()",
                action = () => OwnerPawn?.verbTracker?.InitVerbsFromZero()
            };
            yield return new Command_Action
            {
                defaultLabel = debugZeroCooldown ? "[Dev] 空窗=0 (ON)" : "[Dev] 空窗=0 (OFF)",
                defaultDesc = "切换：将switchCooldownTicks临时设为0（快速切换测试）",
                action = () => debugZeroCooldown = !debugZeroCooldown
            };
            yield return new Command_Action
            {
                defaultLabel = "[Dev] 切换锁定",
                defaultDesc = $"切换allowChipManagement（当前: {Props.allowChipManagement}）",
                action = () => Props.allowChipManagement = !Props.allowChipManagement
            };
            // v3.1：战斗体模拟调试按钮
            yield return new Command_Action
            {
                defaultLabel = "[Dev] 模拟战斗体生成",
                defaultDesc = "IsCombatBodyActive=true → Allocate全部芯片 → ActivateAllSpecial()",
                action = () =>
                {
                    IsCombatBodyActive = true;
                    // Allocate所有已装载芯片的allocationCost
                    var trion = TrionComp;
                    if (trion == null)
                    {
                        Log.Warning("[BDP] 模拟战斗体生成: TrionComp为null（Pawn没有CompTrion？）");
                    }
                    float totalAllocated = 0f;
                    foreach (var slot in AllSlots())
                    {
                        if (slot.loadedChip == null) continue;
                        var chipComp = slot.loadedChip.TryGetComp<TriggerChipComp>();
                        if (chipComp != null && chipComp.Props.allocationCost > 0f)
                        {
                            bool ok = trion?.Allocate(chipComp.Props.allocationCost) ?? false;
                            if (ok)
                                totalAllocated += chipComp.Props.allocationCost;
                            else
                                Log.Warning($"[BDP] Allocate失败: {slot.loadedChip.def.defName} cost={chipComp.Props.allocationCost} available={trion?.Available ?? 0f:F1}");
                        }
                    }
                    ActivateAllSpecial();
                    Log.Message($"[BDP] 模拟战斗体生成完成 (allocated={totalAllocated:F1}, trion={trion?.Cur:F1}/{trion?.Max:F1})");
                }
            };
            yield return new Command_Action
            {
                defaultLabel = "[Dev] 模拟战斗体解除",
                defaultDesc = "DeactivateAll() → Release全部 → IsCombatBodyActive=false",
                action = () =>
                {
                    DeactivateAll();
                    // Release所有已装载芯片的allocationCost
                    var trion = TrionComp;
                    foreach (var slot in AllSlots())
                    {
                        if (slot.loadedChip == null) continue;
                        var chipComp = slot.loadedChip.TryGetComp<TriggerChipComp>();
                        if (chipComp != null && chipComp.Props.allocationCost > 0f)
                            trion?.Release(chipComp.Props.allocationCost);
                    }
                    IsCombatBodyActive = false;
                    Log.Message("[BDP] 模拟战斗体解除完成");
                }
            };
        }

        private void DumpState()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[BDP] CompTriggerBody [{parent.LabelShortCap}] 状态转储:");
            sb.AppendLine($"  switchState={switchState}, pending={pending?.side}#{pending?.slotIndex}");
            sb.AppendLine($"  debugZeroCooldown={debugZeroCooldown}");
            sb.AppendLine($"  dualHandLock={dualHandLockSlot}");
            sb.AppendLine($"  leftVerbProps={leftHandActiveVerbProps?.Count ?? 0}, rightVerbProps={rightHandActiveVerbProps?.Count ?? 0}");
            foreach (var slot in AllSlots())
                sb.AppendLine($"  {slot}");
            var trion = TrionComp;
            if (trion != null)
                sb.AppendLine($"  Trion: cur={trion.Cur:F1} max={trion.Max:F1} avail={trion.Available:F1}");

            // B1诊断：输出VerbTracker.AllVerbs完整列表，帮助诊断Gizmo数量问题
            // v5.1：芯片Verb不在AllVerbs中，单独输出缓存状态
            var pawn = OwnerPawn;
            if (pawn?.equipment?.Primary != null)
            {
                sb.AppendLine($"  === IVerbOwner.VerbProperties ({((IVerbOwner)this).VerbProperties?.Count ?? 0}) ===");
                var ownerVerbs = ((IVerbOwner)this).VerbProperties;
                if (ownerVerbs != null)
                {
                    for (int i = 0; i < ownerVerbs.Count; i++)
                    {
                        var v = ownerVerbs[i];
                        sb.AppendLine($"    [{i}] class={v.verbClass?.Name} primary={v.isPrimary} stdCmd={v.hasStandardCommand} meleeDmgDef={v.meleeDamageDef?.defName} proj={v.defaultProjectile?.defName}");
                    }
                }
                sb.AppendLine($"  === IVerbOwner.Tools ({((IVerbOwner)this).Tools?.Count ?? 0}) ===");
                var ownerTools = ((IVerbOwner)this).Tools;
                if (ownerTools != null)
                {
                    for (int i = 0; i < ownerTools.Count; i++)
                        sb.AppendLine($"    [{i}] label={ownerTools[i].label} dmg={ownerTools[i].power:F1}");
                }
                sb.AppendLine($"  === VerbTracker.AllVerbs ({verbTracker?.AllVerbs?.Count ?? 0}) — 芯片Verb不在此列表 ===");
                if (verbTracker?.AllVerbs != null)
                {
                    for (int i = 0; i < verbTracker.AllVerbs.Count; i++)
                    {
                        var verb = verbTracker.AllVerbs[i];
                        sb.AppendLine($"    [{i}] {verb.GetType().Name} primary={verb.verbProps?.isPrimary} stdCmd={verb.verbProps?.hasStandardCommand} melee={verb.IsMeleeAttack} caster={verb.caster?.LabelShortCap}");
                    }
                }
                // v5.1诊断：输出手动创建的芯片Verb缓存
                sb.AppendLine($"  === 芯片Verb缓存（手动创建） ===");
                sb.AppendLine($"    left: {(leftHandAttackVerb != null ? $"{leftHandAttackVerb.GetType().Name} label={leftHandAttackVerb.verbProps?.label} burst={leftHandAttackVerb.verbProps?.burstShotCount} caster={leftHandAttackVerb.caster?.LabelShortCap}" : "null")}");
                sb.AppendLine($"    right: {(rightHandAttackVerb != null ? $"{rightHandAttackVerb.GetType().Name} label={rightHandAttackVerb.verbProps?.label} burst={rightHandAttackVerb.verbProps?.burstShotCount} caster={rightHandAttackVerb.caster?.LabelShortCap}" : "null")}");
                sb.AppendLine($"    dual: {(dualAttackVerb != null ? $"{dualAttackVerb.GetType().Name} label={dualAttackVerb.verbProps?.label} burst={dualAttackVerb.verbProps?.burstShotCount} caster={dualAttackVerb.caster?.LabelShortCap}" : "null")}");
            }

            Log.Message(sb.ToString());
        }

        private bool HasEmptySlot(SlotSide side)
        {
            // v2.1：支持Special侧
            var slots = side == SlotSide.LeftHand ? leftHandSlots
                      : side == SlotSide.RightHand ? rightHandSlots
                      : specialSlots;
            return slots?.Any(s => s.loadedChip == null) ?? false;
        }

        private void FillRandomChip(SlotSide side)
        {
            var chipDefs = DefDatabase<ThingDef>.AllDefs
                .Where(d => d.comps != null && d.comps.Any(c => c is CompProperties_TriggerChip))
                .ToList();
            if (chipDefs.Count == 0)
            {
                Log.Warning("[BDP] FillRandomChip: 未找到任何TriggerChip ThingDef");
                return;
            }
            // v2.1：支持Special侧
            var slots = side == SlotSide.LeftHand ? leftHandSlots
                      : side == SlotSide.RightHand ? rightHandSlots
                      : specialSlots;
            var emptySlot = slots?.FirstOrDefault(s => s.loadedChip == null);
            if (emptySlot == null) return;
            LoadChipInternal(side, emptySlot.index, ThingMaker.MakeThing(chipDefs.RandomElement()));
        }

        /// <summary>将指定ThingDef的芯片装入指定侧第一个空槽（供右键菜单调用）。</summary>
        private void FillSpecificChip(SlotSide side, ThingDef chipDef)
        {
            var slots = side == SlotSide.LeftHand ? leftHandSlots
                      : side == SlotSide.RightHand ? rightHandSlots
                      : specialSlots;
            var emptySlot = slots?.FirstOrDefault(s => s.loadedChip == null);
            if (emptySlot == null) return;
            LoadChipInternal(side, emptySlot.index, ThingMaker.MakeThing(chipDef));
        }

        /// <summary>生成指定侧的芯片选择菜单项列表（供右键FloatMenu）。</summary>
        private List<FloatMenuOption> GetChipMenuOptions(SlotSide side)
        {
            var chipDefs = DefDatabase<ThingDef>.AllDefs
                .Where(d => d.comps != null && d.comps.Any(c => c is CompProperties_TriggerChip))
                .OrderBy(d => d.defName)
                .ToList();
            var options = new List<FloatMenuOption>(chipDefs.Count);
            foreach (var def in chipDefs)
            {
                var d = def; // 闭包捕获
                options.Add(new FloatMenuOption(
                    $"{d.LabelCap} ({d.defName})",
                    () => FillSpecificChip(side, d)));
            }
            return options;
        }

        private void ClearSide(SlotSide side)
        {
            // v2.1：支持Special侧
            var slots = side == SlotSide.LeftHand ? leftHandSlots
                      : side == SlotSide.RightHand ? rightHandSlots
                      : specialSlots;
            if (slots == null) return;
            foreach (var slot in slots)
            {
                if (slot.isActive) DeactivateSlot(slot);
                slot.loadedChip = null;
            }
        }

        /// <summary>
        /// 支持右键FloatMenu的Command_Action子类。
        /// 左键执行action，右键弹出menuOptions列表。
        /// </summary>
        private class Command_ActionWithMenu : Command_Action
        {
            public List<FloatMenuOption> menuOptions;

            public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
                => menuOptions ?? System.Linq.Enumerable.Empty<FloatMenuOption>();
        }
    }
}
