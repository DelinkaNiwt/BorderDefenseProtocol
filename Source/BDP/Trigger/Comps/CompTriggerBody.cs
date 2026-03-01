using System.Collections.Generic;
using System.Linq;
// System.Reflection已移除：Verb.verbTracker是public字段，无需反射（C4修复）
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
    ///   - 芯片Verb改为在RebuildVerbs中手动创建（Activator.CreateInstance + 直接设置verbTracker）
    ///   - 手动创建的Verb不在VerbTracker.AllVerbs中，彻底隔离于：
    ///     ① Pawn_MeleeVerbs近战选择池（IsMeleeAttack即入池，hasStandardCommand无效）
    ///     ② VerbTracker.GetVerbsCommands Path B（FirstOrDefault(IsMeleeAttack)绑定Y按钮）
    ///   - 芯片Verb只通过Command_BDPChipAttack gizmo使用
    ///
    /// 不变量：
    ///   ① 每侧激活芯片数 ≤ 1（左右手槽）；特殊槽无此限制（全部激活或全部关闭）
    ///   ② 已装载芯片数 ≤ 该侧槽位数
    ///   ③ hasRightHand==false时rightHandSlots为空
    ///   ④ leftSwitchCtx/rightSwitchCtx非null时phase为WindingDown或WarmingUp
    ///   ⑤ leftSwitchCtx/rightSwitchCtx为null时该侧处于Idle
    ///   ⑥ isActive==true的槽位loadedChip!=null
    ///   ⑦ allowChipManagement==false时loadedChip不可被玩家修改
    ///   ⑧ dualHandLockSlot!=null时，另一侧不可激活新芯片（v2.1）
    ///   ⑨ specialSlots全部同时激活/关闭，不参与切换状态机（v2.1）
    ///   ⑩ specialSlotCount==0时specialSlots为null（v2.1）
    ///   ⑪ 特殊槽芯片的激活/关闭必须全部同时进行，不可单独操作（v2.1.1）
    ///   ⑫ activationWarmup对特殊槽芯片无效（战斗体生成时立即激活）（v2.1.1）
    ///   ⑬ IsCombatBodyActive==false时不可激活任何芯片（v6.0）
    ///   ⑭ WindingDown阶段旧芯片仍isActive=true，后摇到期才Deactivate（v6.0）
    /// </summary>
    public partial class CompTriggerBody : CompEquippable, IVerbOwner
    {
        // ── 槽位数据（v2.0：mainSlots/subSlots → leftHandSlots/rightHandSlots） ──
        private List<ChipSlot> leftHandSlots;
        private List<ChipSlot> rightHandSlots;
        // v2.1（T29）：特殊槽，全部同时激活/关闭，不参与切换状态机
        private List<ChipSlot> specialSlots;

        // ── 切换状态机（v6.0：按侧独立，null=Idle） ──
        private SwitchContext leftSwitchCtx;
        private SwitchContext rightSwitchCtx;

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

        // ── v6.1新增：齐射Verb缓存（不序列化，CreateAndCacheChipVerbs后重建） ──
        private Verb leftHandVolleyVerb;    // 左手芯片齐射Verb实例
        private Verb rightHandVolleyVerb;   // 右手芯片齐射Verb实例
        private Verb dualVolleyVerb;        // 双手齐射Verb实例

        // ── v10.0新增：组合技Verb缓存（不序列化，CreateAndCacheChipVerbs后重建） ──
        private Verb comboAttackVerb;       // 组合技攻击Verb实例
        private Verb comboVolleyVerb;       // 组合技齐射Verb实例
        private ComboVerbDef matchedComboDef; // 匹配到的组合技定义（Gizmo用）

        /// <summary>
        /// 芯片Verb序列化列表（v8.0 PMS重构）。
        /// 存档时收集所有芯片Verb，读档时反序列化并注册到LoadedObjectDirectory，
        /// 使Job/Stance在ResolvingCrossRefs阶段能通过loadID找到芯片Verb。
        /// RebuildVerbs时通过loadID匹配复用已反序列化的实例。
        /// </summary>
        private List<Verb> savedChipVerbs;

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
        /// 从当前操作槽位读取指定类型的DefModExtension，回退到遍历所有激活槽位。
        /// 统一替代各Effect类中重复的GetConfig模式（Fix-6）。
        /// </summary>
        public T GetChipExtension<T>() where T : DefModExtension
        {
            // 优先从ActivatingSlot读取（激活/关闭上下文）
            if (ActivatingSlot?.loadedChip != null)
            {
                var cfg = ActivatingSlot.loadedChip.def.GetModExtension<T>();
                if (cfg != null) return cfg;
            }
            // 回退：遍历所有激活槽位（兼容读档恢复等边界情况）
            foreach (var slot in AllActiveSlots())
            {
                var cfg = slot.loadedChip?.def?.GetModExtension<T>();
                if (cfg != null) return cfg;
            }
            return null;
        }

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
        /// 任一侧是否处于切换中（向后兼容属性）。
        /// 懒求值：访问时自动结算到期的阶段。
        /// </summary>
        public bool IsSwitching
        {
            get
            {
                TryResolveSideSwitch(ref leftSwitchCtx, SlotSide.LeftHand);
                TryResolveSideSwitch(ref rightSwitchCtx, SlotSide.RightHand);
                return leftSwitchCtx != null || rightSwitchCtx != null;
            }
        }

        /// <summary>指定侧是否在切换中。</summary>
        public bool IsSideSwitching(SlotSide side)
        {
            if (side == SlotSide.LeftHand)
            {
                TryResolveSideSwitch(ref leftSwitchCtx, SlotSide.LeftHand);
                return leftSwitchCtx != null;
            }
            if (side == SlotSide.RightHand)
            {
                TryResolveSideSwitch(ref rightSwitchCtx, SlotSide.RightHand);
                return rightSwitchCtx != null;
            }
            return false;
        }

        /// <summary>指定侧的切换进度（0=刚开始，1=完成）。</summary>
        public float GetSideSwitchProgress(SlotSide side)
        {
            var ctx = side == SlotSide.LeftHand ? leftSwitchCtx : rightSwitchCtx;
            if (ctx == null) return 1f;

            int now = Find.TickManager.TicksGame;
            int remaining = ctx.phaseEndTick - now;

            if (ctx.phase == SwitchPhase.WindingDown)
            {
                if (ctx.winddownDuration <= 0) return 1f;
                return 1f - Mathf.Clamp01((float)remaining / ctx.winddownDuration);
            }
            else // WarmingUp
            {
                if (ctx.warmupDuration <= 0) return 1f;
                return 1f - Mathf.Clamp01((float)remaining / ctx.warmupDuration);
            }
        }

        /// <summary>指定侧当前切换阶段（供UI区分WindingDown/WarmingUp颜色）。</summary>
        public SwitchPhase GetSideSwitchPhase(SlotSide side)
        {
            var ctx = side == SlotSide.LeftHand ? leftSwitchCtx : rightSwitchCtx;
            return ctx?.phase ?? SwitchPhase.Idle;
        }

        /// <summary>总体切换进度（向后兼容，取两侧中较低的进度）。</summary>
        public float SwitchProgress
        {
            get
            {
                float left = leftSwitchCtx != null ? GetSideSwitchProgress(SlotSide.LeftHand) : 1f;
                float right = rightSwitchCtx != null ? GetSideSwitchProgress(SlotSide.RightHand) : 1f;
                return Mathf.Min(left, right);
            }
        }

        /// <summary>
        /// 懒求值：检查指定侧的切换阶段是否到期，到期则结算。
        /// WindingDown到期 → 关闭旧芯片 → targetSlotIndex≥0时进入WarmingUp，否则回到Idle。
        /// WarmingUp到期 → 激活新芯片 → 回到Idle（ctx=null）。
        /// </summary>
        private void TryResolveSideSwitch(ref SwitchContext ctx, SlotSide side)
        {
            if (ctx == null) return;
            int now = Find.TickManager.TicksGame;
            if (now < ctx.phaseEndTick) return; // 未到期

            if (ctx.phase == SwitchPhase.WindingDown)
            {
                // 后摇到期 → 关闭旧芯片
                var oldSlot = GetSlot(side, ctx.windingDownSlotIndex);
                if (oldSlot != null) DeactivateSlot(oldSlot);

                // 纯关闭（无目标芯片）：后摇到期直接回到Idle
                if (ctx.targetSlotIndex < 0)
                {
                    ctx = null;
                    return;
                }

                // 切换：后摇到期 → 进入新芯片前摇
                var newSlot = GetSlot(side, ctx.targetSlotIndex);
                var newChipComp = newSlot?.loadedChip?.TryGetComp<TriggerChipComp>();
                int warmup = newChipComp?.Props.activationWarmup ?? 0;
                int cooldown = System.Math.Max(Props.switchCooldownTicks, warmup);

                ctx.phase = SwitchPhase.WarmingUp;
                ctx.phaseEndTick = now + cooldown;
                ctx.warmupDuration = cooldown;
                ctx.windingDownSlotIndex = -1;

                // 如果cooldown为0，立即结算WarmingUp
                if (cooldown <= 0)
                {
                    if (CanActivateChip(side, ctx.targetSlotIndex))
                        DoActivate(GetSlot(side, ctx.targetSlotIndex));
                    ctx = null;
                }
            }
            else if (ctx.phase == SwitchPhase.WarmingUp)
            {
                // 前摇到期 → 激活新芯片
                if (CanActivateChip(side, ctx.targetSlotIndex))
                    DoActivate(GetSlot(side, ctx.targetSlotIndex));
                ctx = null; // 回到Idle
            }
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

        /// <summary>获取指定侧的槽位列表（统一替代重复的三元表达式）。</summary>
        private List<ChipSlot> GetSlotsForSide(SlotSide side)
        {
            switch (side)
            {
                case SlotSide.LeftHand:  return leftHandSlots;
                case SlotSide.RightHand: return rightHandSlots;
                case SlotSide.Special:   return specialSlots;
                default:                 return null;
            }
        }

        public ChipSlot GetSlot(SlotSide side, int index)
        {
            var list = GetSlotsForSide(side);
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

        /// <summary>遍历所有已激活且已装载芯片的槽位（手动yield避免LINQ委托分配）。</summary>
        public IEnumerable<ChipSlot> AllActiveSlots()
        {
            if (leftHandSlots != null)
                foreach (var s in leftHandSlots)
                    if (s.isActive && s.loadedChip != null) yield return s;
            if (rightHandSlots != null)
                foreach (var s in rightHandSlots)
                    if (s.isActive && s.loadedChip != null) yield return s;
            if (specialSlots != null)
                foreach (var s in specialSlots)
                    if (s.isActive && s.loadedChip != null) yield return s;
        }

        public bool HasAnyActiveChip()
            => AllSlots().Any(s => s.isActive);

        /// <summary>指定侧是否有激活芯片（§6.3接口约定，供战斗模块检查）。</summary>
        public bool HasActiveChip(SlotSide side)
            => GetActiveSlot(side) != null;

        public ChipSlot GetActiveSlot(SlotSide side)
        {
            return GetSlotsForSide(side)?.FirstOrDefault(s => s.isActive);
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

        // C4修复：Verb.verbTracker是public字段（RimWorld 1.6.4633），无需反射。
        // 原先误以为是internal字段而使用FieldInfo反射访问，实际直接赋值即可。
        // 静态构造函数和fi_verbTracker已移除。

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
        ///   5. 直接设置verb.verbTracker（public字段，使EquipmentSource正确指向触发体）
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
                sb.AppendLine($"    volleyLeft={leftHandVolleyVerb?.GetType().Name} volleyRight={rightHandVolleyVerb?.GetType().Name} volleyDual={dualVolleyVerb?.GetType().Name}");
                sb.AppendLine($"    comboAttack={comboAttackVerb?.GetType().Name} comboVolley={comboVolleyVerb?.GetType().Name} comboDef={matchedComboDef?.defName}");
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
            // v6.1：清空齐射缓存
            leftHandVolleyVerb = null;
            rightHandVolleyVerb = null;
            dualVolleyVerb = null;
            // v10.0：清空组合技缓存
            comboAttackVerb = null;
            comboVolleyVerb = null;
            matchedComboDef = null;

            // 合成芯片VerbProperties
            var chipVerbProps = DualVerbCompositor.ComposeVerbs(
                leftHandActiveVerbProps, rightHandActiveVerbProps,
                GetActiveSlot(SlotSide.LeftHand), GetActiveSlot(SlotSide.RightHand));

            if (chipVerbProps == null) return;

            // 为每个芯片VerbProperties手动创建Verb实例（或复用读档反序列化的实例）
            foreach (var vp in chipVerbProps)
            {
                if (vp.verbClass == null) continue;

                string expectedLoadID = $"BDP_Chip_{parent.ThingID}_{chipVerbProps.IndexOf(vp)}";

                // 读档时优先复用已反序列化的Verb实例（Job/Stance的引用指向该实例）
                Verb verb = FindSavedVerb(expectedLoadID);
                if (verb == null)
                {
                    try
                    {
                        verb = (Verb)System.Activator.CreateInstance(vp.verbClass);
                    }
                    catch (System.Exception ex)
                    {
                        Log.Error($"[BDP] 创建芯片Verb失败: {vp.verbClass.Name} — {ex}");
                        continue;
                    }
                }

                // 模拟VerbTracker.InitVerb的字段设置
                verb.loadID = expectedLoadID;
                verb.verbProps = vp;
                verb.caster = pawn;
                // verb.tool = null（芯片Verb不基于Tool）
                // verb.maneuver = null

                // 直接设置verbTracker（public字段），使verb.EquipmentSource正确指向触发体
                // （EquipmentSource = (DirectOwner as CompEquippable)?.parent）
                verb.verbTracker = VerbTracker;

                // 按type+label分配到缓存槽位
                var vType = verb.GetType();
                var label = vp.label;

                if (verb is Verb_BDPMelee || verb is Verb_BDPShoot)
                {
                    var side = DualVerbCompositor.ParseSideLabel(label);
                    if (side == SlotSide.LeftHand)
                        leftHandAttackVerb = verb;
                    else if (side == SlotSide.RightHand)
                        rightHandAttackVerb = verb;
                    else
                        dualAttackVerb = verb;
                }
                else if (verb is Verb_BDPDualRanged)
                {
                    dualAttackVerb = verb;
                }
            }

            // v6.1：为支持齐射的芯片创建volley verb
            CreateVolleyVerbs(pawn);

            // v10.0：检测组合技匹配，创建组合技Verb
            CreateComboVerbs(pawn);
        }

        /// <summary>
        /// 为支持齐射的远程芯片创建volley verb实例（v6.1新增）。
        /// 遍历已缓存的burst verb，检查对应芯片的supportsVolley标志，
        /// 为支持齐射的芯片创建Verb_BDPVolley/Verb_BDPDualVolley实例。
        ///
        /// 注意：使用GetActiveOrActivatingSlot而非GetActiveSlot。
        /// 原因：DoActivate中effect.Activate()触发RebuildVerbs时，
        ///       slot.isActive尚未设为true，GetActiveSlot找不到正在激活的芯片。
        /// </summary>
        private void CreateVolleyVerbs(Pawn pawn)
        {
            var leftSlot = GetActiveOrActivatingSlot(SlotSide.LeftHand);
            var rightSlot = GetActiveOrActivatingSlot(SlotSide.RightHand);
            var leftCfg = leftSlot?.loadedChip?.def?.GetModExtension<WeaponChipConfig>();
            var rightCfg = rightSlot?.loadedChip?.def?.GetModExtension<WeaponChipConfig>();
            bool leftSupports = leftCfg?.supportsVolley == true && leftCfg.verbProperties != null;
            bool rightSupports = rightCfg?.supportsVolley == true && rightCfg.verbProperties != null;

            // 左手齐射
            if (leftSupports && leftHandAttackVerb != null)
            {
                var volleyType = typeof(Verb_BDPVolley);
                leftHandVolleyVerb = CreateSingleVolleyVerb(leftHandAttackVerb, volleyType, pawn);
            }

            // 右手齐射
            if (rightSupports && rightHandAttackVerb != null)
            {
                var volleyType = typeof(Verb_BDPVolley);
                rightHandVolleyVerb = CreateSingleVolleyVerb(rightHandAttackVerb, volleyType, pawn);
            }

            // 双手齐射：两侧都支持齐射时才创建
            if (leftSupports && rightSupports && dualAttackVerb != null)
                dualVolleyVerb = CreateSingleVolleyVerb(dualAttackVerb, typeof(Verb_BDPDualVolley), pawn);
        }

        /// <summary>
        /// 检测组合技匹配，创建组合技Verb实例（v10.0新增）。
        /// 遍历DefDatabase&lt;ComboVerbDef&gt;，匹配当前左右手激活芯片。
        /// 匹配成功时创建Verb_BDPComboShoot（普通+齐射）。
        /// </summary>
        private void CreateComboVerbs(Pawn pawn)
        {
            var leftSlot = GetActiveOrActivatingSlot(SlotSide.LeftHand);
            var rightSlot = GetActiveOrActivatingSlot(SlotSide.RightHand);
            if (leftSlot?.loadedChip == null || rightSlot?.loadedChip == null) return;

            foreach (var comboDef in DefDatabase<ComboVerbDef>.AllDefs)
            {
                if (!comboDef.Matches(leftSlot.loadedChip.def, rightSlot.loadedChip.def))
                    continue;

                // 匹配成功：创建组合技Verb
                matchedComboDef = comboDef;
                comboAttackVerb = CreateSingleComboVerb(comboDef, false, pawn,
                    leftSlot, rightSlot);
                if (comboDef.supportsVolley)
                    comboVolleyVerb = CreateSingleComboVerb(comboDef, true, pawn,
                        leftSlot, rightSlot);
                break; // 只匹配第一个
            }
        }

        /// <summary>
        /// 创建单个组合技Verb实例（v10.0新增）。
        /// 参数取两侧芯片的平均值。
        /// </summary>
        private Verb CreateSingleComboVerb(ComboVerbDef comboDef, bool isVolley,
            Pawn pawn, ChipSlot leftSlot, ChipSlot rightSlot)
        {
            var leftCfg = leftSlot.loadedChip.def.GetModExtension<WeaponChipConfig>();
            var rightCfg = rightSlot.loadedChip.def.GetModExtension<WeaponChipConfig>();
            if (leftCfg == null || rightCfg == null) return null;

            // 计算平均参数
            float avgRange = (GetFirstRange(leftCfg) + GetFirstRange(rightCfg)) * 0.5f;
            float avgWarmup = (GetFirstWarmup(leftCfg) + GetFirstWarmup(rightCfg)) * 0.5f;
            float avgCooldown = (GetFirstCooldown(leftCfg) + GetFirstCooldown(rightCfg)) * 0.5f;
            int avgBurst = UnityEngine.Mathf.RoundToInt(
                (leftCfg.GetFirstBurstCount() + rightCfg.GetFirstBurstCount()) * 0.5f);
            if (avgBurst < 1) avgBurst = 1;
            float avgTrionCost = (leftCfg.trionCostPerShot + rightCfg.trionCostPerShot) * 0.5f;
            float avgAnchorSpread = (leftCfg.anchorSpread + rightCfg.anchorSpread) * 0.5f;
            float avgVolleySpread = (leftCfg.volleySpreadRadius + rightCfg.volleySpreadRadius) * 0.5f;
            int avgTicksBetween = UnityEngine.Mathf.RoundToInt(
                (GetFirstTicksBetween(leftCfg) + GetFirstTicksBetween(rightCfg)) * 0.5f);

            // 构建VerbProperties
            var vp = new VerbProperties
            {
                verbClass = typeof(Verb_BDPComboShoot),
                isPrimary = false,
                hasStandardCommand = false,
                defaultProjectile = comboDef.projectileDef,
                soundCast = GetFirstSound(leftCfg) ?? GetFirstSound(rightCfg),
                muzzleFlashScale = 10f,
                range = avgRange,
                warmupTime = avgWarmup,
                defaultCooldownTime = avgCooldown,
                // 齐射模式：burstShotCount=1（TryCastShot内循环）
                // 普通模式：burstShotCount=avgBurst（引擎burst机制）
                burstShotCount = isVolley ? 1 : avgBurst,
                ticksBetweenBurstShots = isVolley ? 0 : avgTicksBetween,
            };

            string suffix = isVolley ? "Volley" : "Attack";
            string expectedLoadID = $"BDP_Combo_{parent.ThingID}_{comboDef.defName}_{suffix}";

            // 读档时优先复用已反序列化的Verb实例
            Verb verb = FindSavedVerb(expectedLoadID);
            if (verb == null)
            {
                try
                {
                    verb = (Verb)System.Activator.CreateInstance(typeof(Verb_BDPComboShoot));
                }
                catch (System.Exception ex)
                {
                    Log.Error($"[BDP] 创建组合技Verb失败: {ex}");
                    return null;
                }
            }

            verb.loadID = expectedLoadID;
            verb.verbProps = vp;
            verb.caster = pawn;
            verb.verbTracker = VerbTracker;

            // 设置组合技专用字段
            var comboVerb = (Verb_BDPComboShoot)verb;
            comboVerb.comboDef = comboDef;
            comboVerb.isVolley = isVolley;
            comboVerb.avgBurstCount = avgBurst;
            comboVerb.avgTrionCost = avgTrionCost;
            comboVerb.avgAnchorSpread = avgAnchorSpread;
            comboVerb.avgVolleySpread = avgVolleySpread;

            return verb;
        }

        // ── 组合技参数读取辅助（从WeaponChipConfig.verbProperties[0]读取） ──

        private static float GetFirstRange(WeaponChipConfig cfg)
        {
            if (cfg?.verbProperties != null)
                foreach (var vp in cfg.verbProperties)
                    if (vp.range > 0) return vp.range;
            return 20f;
        }

        private static float GetFirstWarmup(WeaponChipConfig cfg)
        {
            if (cfg?.verbProperties != null)
                foreach (var vp in cfg.verbProperties)
                    return vp.warmupTime;
            return 1f;
        }

        private static float GetFirstCooldown(WeaponChipConfig cfg)
        {
            if (cfg?.verbProperties != null)
                foreach (var vp in cfg.verbProperties)
                    return vp.defaultCooldownTime;
            return 1f;
        }

        private static int GetFirstTicksBetween(WeaponChipConfig cfg)
        {
            if (cfg?.verbProperties != null)
                foreach (var vp in cfg.verbProperties)
                    return vp.ticksBetweenBurstShots;
            return 8;
        }

        private static SoundDef GetFirstSound(WeaponChipConfig cfg)
        {
            if (cfg?.verbProperties != null)
                foreach (var vp in cfg.verbProperties)
                    if (vp.soundCast != null) return vp.soundCast;
            return null;
        }

        /// <summary>
        /// 获取指定侧的激活槽位，包含正在激活中的槽位（ActivatingSlot）。
        /// 原因：DoActivate中slot.isActive在effect.Activate()之后才设为true，
        ///       但RebuildVerbs在effect.Activate()内部调用，此时GetActiveSlot找不到该槽位。
        /// </summary>
        private ChipSlot GetActiveOrActivatingSlot(SlotSide side)
        {
            var active = GetActiveSlot(side);
            if (active != null) return active;
            // 回退：检查正在激活的槽位是否属于该侧
            if (ActivatingSlot != null && ActivatingSlot.side == side)
                return ActivatingSlot;
            return null;
        }

        /// <summary>
        /// 基于已有burst verb的VerbProperties创建一个volley verb实例。
        /// 复制VerbProperties，修改verbClass和burstShotCount=1。
        /// </summary>
        private Verb CreateSingleVolleyVerb(Verb sourceVerb, System.Type volleyVerbClass, Pawn pawn)
        {
            var srcVp = sourceVerb.verbProps;

            // 复制VerbProperties，修改verbClass和burstShotCount
            var volleyVp = new VerbProperties
            {
                verbClass = volleyVerbClass,
                isPrimary = srcVp.isPrimary,
                hasStandardCommand = false,
                defaultProjectile = srcVp.defaultProjectile,
                soundCast = srcVp.soundCast,
                muzzleFlashScale = srcVp.muzzleFlashScale,
                ticksBetweenBurstShots = 0,
                range = srcVp.range,
                warmupTime = srcVp.warmupTime,
                defaultCooldownTime = srcVp.defaultCooldownTime,
                burstShotCount = 1, // 引擎只调用一次TryCastShot
                label = srcVp.label,
                meleeDamageDef = srcVp.meleeDamageDef,
                meleeDamageBaseAmount = srcVp.meleeDamageBaseAmount,
            };

            string expectedLoadID = $"BDP_Volley_{parent.ThingID}_{volleyVerbClass.Name}_{srcVp.label}";

            // 读档时优先复用已反序列化的Verb实例
            Verb verb = FindSavedVerb(expectedLoadID);
            if (verb == null)
            {
                try
                {
                    verb = (Verb)System.Activator.CreateInstance(volleyVerbClass);
                }
                catch (System.Exception ex)
                {
                    Log.Error($"[BDP] 创建齐射Verb失败: {volleyVerbClass.Name} — {ex}");
                    return null;
                }
            }

            verb.loadID = expectedLoadID;
            verb.verbProps = volleyVp;
            verb.caster = pawn;
            verb.verbTracker = VerbTracker;

            return verb;
        }

        // ── 读档Verb复用（v8.0 PMS重构） ──

        /// <summary>
        /// 从读档反序列化的Verb列表中查找匹配loadID的实例。
        /// 找到后从列表移除（避免重复匹配）。
        /// </summary>
        private Verb FindSavedVerb(string loadID)
        {
            if (savedChipVerbs == null) return null;
            for (int i = 0; i < savedChipVerbs.Count; i++)
            {
                if (savedChipVerbs[i]?.loadID == loadID)
                {
                    var verb = savedChipVerbs[i];
                    savedChipVerbs.RemoveAt(i);
                    return verb;
                }
            }
            return null;
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
            // v6.0：战斗体未激活时不可激活任何芯片（不变量⑬）
            if (!IsCombatBodyActive) return false;

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

        /// <summary>
        /// 激活指定槽位（v6.0重写：按侧独立状态机 + 必须前摇 + 后摇）。
        /// 所有激活都必须走前摇（WarmingUp），不再有直接DoActivate路径。
        /// 有旧芯片时：后摇(WindingDown) → 前摇(WarmingUp)。
        /// 无旧芯片时：直接进入前摇(WarmingUp)。
        /// </summary>
        public bool ActivateChip(SlotSide side, int slotIndex)
        {
            var slot = GetSlot(side, slotIndex);
            if (slot?.loadedChip == null) return false;
            if (!CanActivateChip(side, slotIndex)) return false;

            // v2.1：Special侧不走切换逻辑，委托给ActivateAllSpecial
            if (side == SlotSide.Special)
            {
                ActivateAllSpecial();
                return true;
            }

            // 本侧正在切换中，拒绝新请求
            if (IsSideSwitching(side)) return false;

            var chipComp = slot.loadedChip.TryGetComp<TriggerChipComp>();

            // v2.1（T31）：双手芯片——检查两侧都不在切换中
            if (chipComp?.Props.isDualHand == true)
            {
                if (IsSideSwitching(SlotSide.LeftHand) || IsSideSwitching(SlotSide.RightHand))
                    return false;

                var oppositeSide = side == SlotSide.LeftHand ? SlotSide.RightHand : SlotSide.LeftHand;
                var oppositeActive = GetActiveSlot(oppositeSide);
                if (oppositeActive != null)
                    DeactivateSlot(oppositeActive);
                var existingDual = GetActiveSlot(side);
                if (existingDual != null && existingDual != slot)
                    DeactivateSlot(existingDual);
                DoActivate(slot);
                return true;
            }

            // 获取本侧SwitchContext引用
            int now = Find.TickManager.TicksGame;
            var existingActive = GetActiveSlot(side);

            if (existingActive != null && existingActive != slot)
            {
                // 有旧芯片：检查后摇
                var oldChipComp = existingActive.loadedChip?.TryGetComp<TriggerChipComp>();
                int winddown = oldChipComp?.Props.deactivationDelay ?? 0;

                var ctx = new SwitchContext
                {
                    targetSlotIndex = slotIndex,
                };

                if (winddown > 0)
                {
                    // 进入后摇阶段（旧芯片仍isActive=true）
                    ctx.phase = SwitchPhase.WindingDown;
                    ctx.phaseEndTick = now + winddown;
                    ctx.winddownDuration = winddown;
                    ctx.windingDownSlotIndex = existingActive.index;
                }
                else
                {
                    // 无后摇：立即关闭旧芯片，进入前摇
                    DeactivateSlot(existingActive);
                    int warmup = chipComp?.Props.activationWarmup ?? 0;
                    int cooldown = System.Math.Max(Props.switchCooldownTicks, warmup);
                    ctx.phase = SwitchPhase.WarmingUp;
                    ctx.phaseEndTick = now + cooldown;
                    ctx.warmupDuration = cooldown;
                }

                SetSideCtx(side, ctx);
            }
            else
            {
                // 无旧芯片：直接进入前摇
                int warmup = chipComp?.Props.activationWarmup ?? 0;
                int cooldown = System.Math.Max(Props.switchCooldownTicks, warmup);

                var ctx = new SwitchContext
                {
                    phase = SwitchPhase.WarmingUp,
                    phaseEndTick = now + cooldown,
                    warmupDuration = cooldown,
                    targetSlotIndex = slotIndex,
                };

                SetSideCtx(side, ctx);

                // cooldown为0时立即结算
                if (cooldown <= 0)
                {
                    if (CanActivateChip(side, slotIndex))
                        DoActivate(slot);
                    SetSideCtx(side, null);
                }
            }

            return true;
        }

        /// <summary>设置指定侧的SwitchContext。</summary>
        private void SetSideCtx(SlotSide side, SwitchContext ctx)
        {
            if (side == SlotSide.LeftHand) leftSwitchCtx = ctx;
            else if (side == SlotSide.RightHand) rightSwitchCtx = ctx;
        }

        /// <summary>
        /// 关闭指定侧的当前激活芯片（v6.0：支持后摇）。
        /// 有deactivationDelay时进入WindingDown阶段（芯片仍isActive=true），到期才真正关闭。
        /// 无deactivationDelay时立即关闭。
        /// </summary>
        public void DeactivateChip(SlotSide side)
        {
            var active = GetActiveSlot(side);
            if (active == null) return;

            // 该侧已在切换/后摇中，拒绝重复操作
            if (IsSideSwitching(side)) return;

            var chipComp = active.loadedChip?.TryGetComp<TriggerChipComp>();
            int winddown = chipComp?.Props.deactivationDelay ?? 0;

            if (winddown > 0)
            {
                // 进入后摇阶段（芯片仍isActive=true，后摇到期才真正关闭）
                int now = Find.TickManager.TicksGame;
                var ctx = new SwitchContext
                {
                    phase = SwitchPhase.WindingDown,
                    phaseEndTick = now + winddown,
                    winddownDuration = winddown,
                    windingDownSlotIndex = active.index,
                    targetSlotIndex = -1, // 无目标：纯关闭，不接前摇
                };
                SetSideCtx(side, ctx);
            }
            else
            {
                // 无后摇：立即关闭
                DeactivateSlot(active);
            }
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

            // v6.0：清除两侧切换上下文
            leftSwitchCtx = null;
            rightSwitchCtx = null;
            // v2.1：清除双手锁定
            dualHandLockSlot = null;
        }

        /// <summary>
        /// 激活所有特殊槽（全部同时激活）。
        /// 特殊槽不参与切换状态机，不受切换冷却影响（不变量⑨⑫）。
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
            // C3修复：try/finally保护，防止effect.Activate异常导致上下文残留
            ActivatingSide = slot.side;
            ActivatingSlot = slot;
            try
            {
                effect.Activate(pawn, parent);
            }
            finally
            {
                ActivatingSide = null;
                ActivatingSlot = null;
            }

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
            // C3修复：try/finally保护，防止effect.Deactivate异常导致上下文残留
            ActivatingSide = slot.side;
            ActivatingSlot = slot;
            try
            {
                effect?.Deactivate(pawn, parent);
            }
            finally
            {
                ActivatingSide = null;
                ActivatingSlot = null;
            }

            slot.isActive = false;

            // ── v2.1（T31）：清除双手锁定 ──
            if (dualHandLockSlot == slot)
                dualHandLockSlot = null;

            // ── v4.0（F1）：组合能力移除（芯片关闭后重新检查） ──
            TryRevokeComboAbilities(pawn);
        }

        // ═══════════════════════════════════════════
        //  战斗体管理
        // ═══════════════════════════════════════════

        /// <summary>
        /// 解除战斗体：关闭所有芯片 → 释放全部Trion占用 → 标记未激活。
        /// 释放逻辑基于trion.Allocated（Single Source of Truth），不依赖芯片是否仍在槽位中。
        /// </summary>
        public void DismissCombatBody()
        {
            DeactivateAll();
            var trion = TrionComp;
            if (trion != null) trion.Release(trion.Allocated);
            IsCombatBodyActive = false;
        }

        /// <summary>
        /// 检查当前Trion是否足够生成战斗体（所有已装载芯片的allocationCost总和）。
        /// </summary>
        public bool CanGenerateCombatBody()
        {
            var trion = TrionComp;
            if (trion == null) return false;
            float totalCost = 0f;
            foreach (var slot in AllSlots())
            {
                if (slot.loadedChip == null) continue;
                var chipComp = slot.loadedChip.TryGetComp<TriggerChipComp>();
                if (chipComp != null)
                    totalCost += chipComp.Props.allocationCost;
            }
            return trion.Available >= totalCost;
        }

        /// <summary>
        /// Trion可用值耗尽回调——自动解除战斗体。
        /// 由CompTrion.OnAvailableDepleted事件触发。
        /// </summary>
        private void OnTrionDepleted()
        {
            if (!IsCombatBodyActive) return;
            DismissCombatBody();
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

            // v6.0：注册Trion耗尽事件
            var trion = TrionComp;
            if (trion != null) trion.OnAvailableDepleted += OnTrionDepleted;

            if (Props.autoActivateOnEquip)
            {
                foreach (var slot in AllSlots())
                    if (slot.loadedChip != null && !slot.isActive)
                        ActivateChip(slot.side, slot.index);
            }
        }

        public override void Notify_Unequipped(Pawn pawn)
        {
            // v6.0：注销Trion耗尽事件（pawn参数即卸下前的持有者）
            var trion = pawn?.GetComp<CompTrion>();
            if (trion != null) trion.OnAvailableDepleted -= OnTrionDepleted;

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
            // v6.0：按侧独立切换上下文（旧存档中不存在，Scribe_Deep返回null即Idle）
            Scribe_Deep.Look(ref leftSwitchCtx, "leftSwitchCtx");
            Scribe_Deep.Look(ref rightSwitchCtx, "rightSwitchCtx");
            // dualHandLockSlot是运行时引用，不序列化，读档后由激活恢复逻辑重建

            // v3.1：序列化IsCombatBodyActive（战斗体状态需跨存档保持）
            bool isCombatBodyActive = IsCombatBodyActive;
            Scribe_Values.Look(ref isCombatBodyActive, "isCombatBodyActive");
            IsCombatBodyActive = isCombatBodyActive;

            // v8.0 PMS重构：序列化芯片Verb，使读档时Job/Stance能解析Verb引用
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                savedChipVerbs = new List<Verb>();
                if (leftHandAttackVerb != null) savedChipVerbs.Add(leftHandAttackVerb);
                if (rightHandAttackVerb != null) savedChipVerbs.Add(rightHandAttackVerb);
                if (dualAttackVerb != null) savedChipVerbs.Add(dualAttackVerb);
                if (leftHandVolleyVerb != null) savedChipVerbs.Add(leftHandVolleyVerb);
                if (rightHandVolleyVerb != null) savedChipVerbs.Add(rightHandVolleyVerb);
                if (dualVolleyVerb != null) savedChipVerbs.Add(dualVolleyVerb);
                // v10.0：组合技Verb
                if (comboAttackVerb != null) savedChipVerbs.Add(comboAttackVerb);
                if (comboVolleyVerb != null) savedChipVerbs.Add(comboVolleyVerb);
            }
            Scribe_Collections.Look(ref savedChipVerbs, "chipVerbs", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (leftHandSlots == null) leftHandSlots = InitSlots(SlotSide.LeftHand, Props.leftHandSlotCount);
                if (Props.hasRightHand && rightHandSlots == null) rightHandSlots = InitSlots(SlotSide.RightHand, Props.rightHandSlotCount);
                // v2.1：特殊槽读档恢复
                if (Props.specialSlotCount > 0 && specialSlots == null)
                    specialSlots = InitSlots(SlotSide.Special, Props.specialSlotCount);

                // 恢复激活效果（IChipEffect无状态，重新调用Activate()）
                var pawn = OwnerPawn;
                if (pawn != null)
                {
                    // v6.0：读档后重新注册Trion耗尽事件
                    var trion = pawn.GetComp<CompTrion>();
                    if (trion != null) trion.OnAvailableDepleted += OnTrionDepleted;

                    foreach (var slot in AllSlots())
                    {
                        if (slot.isActive && slot.loadedChip != null)
                        {
                            var chipComp = slot.loadedChip.TryGetComp<TriggerChipComp>();
                            var effect = chipComp?.GetEffect();
                            // C3修复：try/finally保护读档恢复路径
                            ActivatingSide = slot.side;
                            ActivatingSlot = slot;
                            try
                            {
                                effect?.Activate(pawn, parent);
                            }
                            finally
                            {
                                ActivatingSide = null;
                                ActivatingSlot = null;
                            }

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

            // ── v5.0新增：芯片攻击Gizmo（仅征召时显示，减少UI噪音） ──
            bool drafted = OwnerPawn?.Drafted == true;
            if (drafted)
            {
                var leftSlot = GetActiveSlot(SlotSide.LeftHand);
                var rightSlot = GetActiveSlot(SlotSide.RightHand);
                var leftChipDef = leftSlot?.loadedChip?.def;
                var rightChipDef = rightSlot?.loadedChip?.def;

                if (leftHandAttackVerb != null && leftChipDef != null)
                {
                    yield return new Command_BDPChipAttack
                    {
                        verb = leftHandAttackVerb,
                        volleyVerb = leftHandVolleyVerb, // v6.1：齐射
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
                        volleyVerb = rightHandVolleyVerb, // v6.1：齐射
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
                        volleyVerb = dualVolleyVerb, // v6.1：齐射
                        attackId = "dual:" + a + "+" + b,
                        icon = parent.def.uiIcon, // 触发体图标
                        defaultLabel = "双手触发",
                    };
                }

                // v10.0：组合技Gizmo（B+C同时激活时显示）
                if (comboAttackVerb != null && matchedComboDef != null)
                {
                    yield return new Command_BDPChipAttack
                    {
                        verb = comboAttackVerb,
                        volleyVerb = comboVolleyVerb,
                        attackId = "combo:" + matchedComboDef.defName,
                        icon = parent.def.uiIcon, // 暂用触发体图标
                        defaultLabel = matchedComboDef.label ?? "组合技",
                    };
                }
            }

            // v9.0：射击模式Gizmo（始终显示，方便战前配置）
            foreach (var slot in AllActiveSlots())
            {
                var fm = slot.loadedChip?.TryGetComp<CompFireMode>();
                if (fm != null)
                    yield return new Gizmo_FireMode(fm, slot.loadedChip.def.label);
            }

            // v2.1.1：allowChipManagement=false时不显示状态Gizmo
            // 原因：近界/黑触发器玩家无法操作芯片，显示Gizmo无意义
            if (Props.allowChipManagement)
                yield return new Gizmo_TriggerBodyStatus(this);

            // v3.1：战斗体模拟按钮（godMode守卫，避免正式游戏中暴露调试功能）
            if (DebugSettings.godMode)
                foreach (var g in GetCombatBodyGizmos()) yield return g;

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

        // ── 调试/开发工具方法已提取到 CompTriggerBody.Debug.cs（Fix-8：partial class） ──
    }
}
