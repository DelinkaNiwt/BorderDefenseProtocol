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
    ///
    /// v11.0变更（战斗体系统重构）：
    ///   - 实现ICombatBodySupport接口，消除反射调用
    ///   - 提供类型安全的战斗体支持API
    /// </summary>
    public partial class CompTriggerBody : CompEquippable, IVerbOwner, ICombatBodySupport
    {
        // ── 字段声明已移至 CompTriggerBody.Fields.cs ──

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
                sb.AppendLine($"    secondaryLeft={leftHandSecondaryVerb?.GetType().Name} secondaryRight={rightHandSecondaryVerb?.GetType().Name} secondaryDual={dualSecondaryVerb?.GetType().Name}");
                sb.AppendLine($"    comboAttack={comboAttackVerb?.GetType().Name} comboSecondary={comboSecondaryVerb?.GetType().Name} comboDef={matchedComboDef?.defName}");
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
            // v6.1：清空副攻击缓存（v8.0重命名，v9.0统一语义）
            leftHandSecondaryVerb = null;
            rightHandSecondaryVerb = null;
            dualSecondaryVerb = null;
            // v10.0：清空组合技缓存（v9.0重命名）
            comboAttackVerb = null;
            comboSecondaryVerb = null;
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
            // v8.0变更：支持primaryVerbProps/secondaryVerbProps配置
            CreateSecondaryVerbs(pawn);

            // v10.0：检测组合技匹配，创建组合技Verb
            CreateComboVerbs(pawn);
        }

        /// <summary>
        /// 为芯片创建副攻击verb实例（v8.0重构）。
        ///
        /// 新逻辑（v8.0）：
        ///   1. 优先使用 secondaryVerbProps 显式配置的副攻击verb
        ///   2. 如果 secondaryVerbProps == null 且 supportsVolley == true，创建齐射verb（向后兼容）
        ///   3. 如果 secondaryVerbProps == null 且 supportsVolley == false，副攻击为null（右键走默认行为）
        ///
        /// 旧逻辑（v6.1）：
        ///   遍历已缓存的burst verb，检查对应芯片的supportsVolley标志，
        ///   为支持齐射的芯片创建Verb_BDPVolley/Verb_BDPDualVolley实例。
        ///
        /// 注意：使用GetActiveOrActivatingSlot而非GetActiveSlot。
        /// 原因：DoActivate中effect.Activate()触发RebuildVerbs时，
        ///       slot.isActive尚未设为true，GetActiveSlot找不到正在激活的芯片。
        /// </summary>
        private void CreateSecondaryVerbs(Pawn pawn)
        {
            var leftSlot = GetActiveOrActivatingSlot(SlotSide.LeftHand);
            var rightSlot = GetActiveOrActivatingSlot(SlotSide.RightHand);
            var leftCfg = leftSlot?.loadedChip?.def?.GetModExtension<WeaponChipConfig>();
            var rightCfg = rightSlot?.loadedChip?.def?.GetModExtension<WeaponChipConfig>();

            // 左手副攻击
            if (leftHandAttackVerb != null && leftCfg != null)
            {
                leftHandSecondaryVerb = CreateSecondaryVerb(leftCfg, leftHandAttackVerb, pawn, "Left");
            }

            // 右手副攻击
            if (rightHandAttackVerb != null && rightCfg != null)
            {
                rightHandSecondaryVerb = CreateSecondaryVerb(rightCfg, rightHandAttackVerb, pawn, "Right");
            }

            // 双手副攻击：两侧都有副攻击时才创建
            if (leftHandSecondaryVerb != null && rightHandSecondaryVerb != null && dualAttackVerb != null)
            {
                dualSecondaryVerb = CreateSecondaryVerb(leftCfg, dualAttackVerb, pawn, "Dual");
            }
        }

        /// <summary>
        /// 创建单个副攻击verb实例（v9.0重构）。
        ///
        /// 逻辑：
        ///   1. 如果 cfg.secondaryVerbProps != null，使用它创建verb
        ///   2. 否则如果 cfg.supportsVolley == true，创建齐射verb（向后兼容）
        ///   3. 否则返回null
        /// </summary>
        private Verb CreateSecondaryVerb(WeaponChipConfig cfg, Verb primaryVerb, Pawn pawn, string side)
        {
            // 优先使用显式配置的secondaryVerbProps
            if (cfg.secondaryVerbProps != null)
            {
                string loadID = $"BDP_Secondary{side}_{parent.ThingID}_{cfg.secondaryVerbProps.label}";
                return FindOrCreateVerb(cfg.secondaryVerbProps, pawn, loadID);
            }

            // 向后兼容：supportsVolley=true时自动创建齐射verb
            if (cfg.supportsVolley)
            {
                return CreateLegacyVolleyVerb(primaryVerb, pawn, side);
            }

            return null;
        }

        /// <summary>
        /// 统一的Verb查找或创建方法（v9.0新增）。
        /// 读档时优先复用已反序列化的Verb实例，否则创建新实例。
        /// </summary>
        private Verb FindOrCreateVerb(VerbProperties vp, Pawn pawn, string loadID)
        {
            if (vp.verbClass == null) return null;

            // 读档时优先复用已反序列化的Verb实例
            Verb verb = FindSavedVerb(loadID);
            if (verb == null)
            {
                try
                {
                    verb = (Verb)System.Activator.CreateInstance(vp.verbClass);
                }
                catch (System.Exception ex)
                {
                    Log.Error($"[BDP] 创建Verb失败: {vp.verbClass.Name} — {ex}");
                    return null;
                }
            }

            // 初始化Verb
            verb.loadID = loadID;
            verb.verbProps = vp;
            verb.caster = pawn;
            verb.verbTracker = VerbTracker;

            return verb;
        }

        /// <summary>
        /// 向后兼容方法：基于primaryVerb自动创建齐射verb（v9.0标记为过时）。
        /// </summary>
        [System.Obsolete("Use explicit secondaryVerbProps configuration instead")]
        private Verb CreateLegacyVolleyVerb(Verb primaryVerb, Pawn pawn, string side)
        {
            var srcVp = primaryVerb.verbProps;

            // 确定齐射verb类型
            System.Type volleyVerbClass = side == "Dual" ? typeof(Verb_BDPDualVolley) : typeof(Verb_BDPVolley);

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
                label = "齐射", // 固定label
                meleeDamageDef = srcVp.meleeDamageDef,
                meleeDamageBaseAmount = srcVp.meleeDamageBaseAmount,
            };

            string loadID = $"BDP_LegacyVolley{side}_{parent.ThingID}_{volleyVerbClass.Name}";
            return FindOrCreateVerb(volleyVp, pawn, loadID);
        }

        /// <summary>
        /// 检测组合技匹配，创建组合技Verb实例（v10.0新增，v9.0重构）。
        /// 遍历DefDatabase&lt;ComboVerbDef&gt;，匹配当前左右手激活芯片。
        /// 匹配成功时创建主攻击verb和副攻击verb（如果配置）。
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

                // 创建主攻击verb
                comboAttackVerb = CreateComboVerb(comboDef, false, pawn, leftSlot, rightSlot);

                // 创建副攻击verb
                comboSecondaryVerb = CreateComboVerb(comboDef, true, pawn, leftSlot, rightSlot);

                break; // 只匹配第一个
            }
        }

        /// <summary>
        /// 创建单个组合技Verb实例（v10.0新增，v9.0重构）。
        /// 参数取两侧芯片的平均值。
        /// </summary>
        private Verb CreateComboVerb(ComboVerbDef comboDef, bool isSecondary,
            Pawn pawn, ChipSlot leftSlot, ChipSlot rightSlot)
        {
            var leftCfg = leftSlot.loadedChip.def.GetModExtension<WeaponChipConfig>();
            var rightCfg = rightSlot.loadedChip.def.GetModExtension<WeaponChipConfig>();
            if (leftCfg == null || rightCfg == null) return null;

            // 如果是副攻击但未配置，检查向后兼容
            if (isSecondary)
            {
                // 优先使用secondaryVerbClass
                if (comboDef.secondaryVerbClass != null)
                {
                    // 创建显式配置的副攻击verb
                    return CreateComboVerbFromClass(comboDef, comboDef.secondaryVerbClass,
                        pawn, leftCfg, rightCfg, true);
                }
                // 向后兼容：supportsVolley=true时自动创建齐射verb
                else if (comboDef.supportsVolley)
                {
                    return CreateComboVerbFromClass(comboDef, typeof(Verb_BDPComboShoot),
                        pawn, leftCfg, rightCfg, true);
                }
                else
                {
                    return null; // 无副攻击
                }
            }

            // 主攻击逻辑：使用primaryVerbClass
            return CreateComboVerbFromClass(comboDef, comboDef.primaryVerbClass,
                pawn, leftCfg, rightCfg, false);
        }

        /// <summary>
        /// 从指定Verb类型创建组合技Verb实例（v9.0新增）。
        /// </summary>
        private Verb CreateComboVerbFromClass(ComboVerbDef comboDef, System.Type verbClass,
            Pawn pawn, WeaponChipConfig leftCfg, WeaponChipConfig rightCfg, bool isSecondary)
        {
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
                verbClass = verbClass,
                isPrimary = false,
                hasStandardCommand = false,
                defaultProjectile = comboDef.projectileDef,
                soundCast = GetFirstSound(leftCfg) ?? GetFirstSound(rightCfg),
                muzzleFlashScale = 10f,
                range = avgRange,
                warmupTime = avgWarmup,
                defaultCooldownTime = avgCooldown,
                // 副攻击模式：burstShotCount=1（TryCastShot内循环）
                // 主攻击模式：burstShotCount=avgBurst（引擎burst机制）
                burstShotCount = isSecondary ? 1 : avgBurst,
                ticksBetweenBurstShots = isSecondary ? 0 : avgTicksBetween,
                label = isSecondary ? "组合技副攻击" : comboDef.label,
            };

            string suffix = isSecondary ? "Secondary" : "Attack";
            string loadID = $"BDP_Combo_{parent.ThingID}_{comboDef.defName}_{suffix}";

            // 使用统一的FindOrCreateVerb方法
            Verb verb = FindOrCreateVerb(vp, pawn, loadID);
            if (verb == null) return null;

            // 设置组合技专用字段（如果是Verb_BDPComboShoot）
            if (verb is Verb_BDPComboShoot comboVerb)
            {
                comboVerb.comboDef = comboDef;
                comboVerb.isVolley = isSecondary;
                comboVerb.avgBurstCount = avgBurst;
                comboVerb.avgTrionCost = avgTrionCost;
                comboVerb.avgAnchorSpread = avgAnchorSpread;
                comboVerb.avgVolleySpread = avgVolleySpread;
            }

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
        //  预占用值同步（v1.8）
        // ═══════════════════════════════════════════

        /// <summary>
        /// 计算所有已装载芯片的总Trion占用量。
        /// </summary>
        private float CalculateTotalAllocationCost()
        {
            float total = 0f;

            foreach (var slot in AllSlots())
            {
                if (slot.loadedChip != null)
                {
                    var chipComp = slot.loadedChip.TryGetComp<TriggerChipComp>();
                    if (chipComp != null)
                    {
                        total += chipComp.Props.allocationCost;
                    }
                }
            }

            return total;
        }

        /// <summary>
        /// 同步预占用值到CompTrion。
        /// 在芯片装载/卸载、装备/卸下时调用。
        /// </summary>
        private void SyncReservedAllocationToTrion()
        {
            var compTrion = OwnerPawn?.GetComp<CompTrion>();
            if (compTrion != null)
            {
                float total = CalculateTotalAllocationCost();
                compTrion.UpdateReservedAllocation(total);
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

            var chipComp = chip?.TryGetComp<TriggerChipComp>();
            if (chipComp == null) return false;

            // 检查槽位限制（v3.1：改用枚举）
            var chipProps = chipComp.Props as CompProperties_TriggerChip;
            if (chipProps != null)
            {
                var restriction = chipProps.slotRestriction;
                var currentSlot = slot.side;

                // 检查槽位限制
                bool canInsert = true;
                if (restriction == ChipSlotRestriction.SpecialOnly)
                {
                    canInsert = (currentSlot == SlotSide.Special);
                }
                else if (restriction == ChipSlotRestriction.HandsOnly)
                {
                    canInsert = (currentSlot == SlotSide.LeftHand || currentSlot == SlotSide.RightHand);
                }
                // ChipSlotRestriction.None 时 canInsert 保持 true

                if (!canInsert)
                {
                    string restrictionDesc = "未知槽位";
                    if (restriction == ChipSlotRestriction.SpecialOnly)
                    {
                        restrictionDesc = "特殊槽位";
                    }
                    else if (restriction == ChipSlotRestriction.HandsOnly)
                    {
                        restrictionDesc = "左右手槽位";
                    }

                    Messages.Message(
                        $"该芯片只能插入{restrictionDesc}",
                        MessageTypeDefOf.RejectInput);
                    return false;
                }
            }

            slot.loadedChip = chip;
            chip.holdingOwner?.Remove(chip);

            // v1.8：装载后同步预占用值
            SyncReservedAllocationToTrion();

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

            // v1.8：卸载后同步预占用值
            SyncReservedAllocationToTrion();

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

            // 槽位被禁用（手部/手臂被毁）时不可激活
            if (slot.isDisabled) return false;

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
        /// 尝试为战斗体占用Trion（v11.1修复：添加原子性保证）。
        /// 由Gene_TrionGland在战斗体激活前调用，用于检查并锁定Trion。
        ///
        /// 流程：
        /// 1. 计算总需求量
        /// 2. 检查是否足够（原子性检查）
        /// 3. 如果足够，设置标志并逐个Allocate；如果不足，返回false
        ///
        /// 返回值：
        /// - true: 所有芯片成功Allocate，可以激活战斗体
        /// - false: Trion不足或TrionComp不存在，不应激活
        ///
        /// 注意：此方法只负责Allocate，不激活特殊槽芯片（由ActivateAllSpecial单独调用）。
        /// </summary>
        public bool TryAllocateTrionForCombatBody()
        {

            var trion = TrionComp;
            if (trion == null)
            {
                Log.Error($"[BDP] TryAllocateTrionForCombatBody失败: TrionComp为null");
                return false;
            }

            // 1. 计算总需求量
            float totalCost = 0f;
            var slotsWithCost = new List<(ChipSlot slot, float cost)>();

            foreach (var slot in AllSlots())
            {
                if (slot.loadedChip == null) continue;
                var chipComp = slot.loadedChip.TryGetComp<TriggerChipComp>();
                if (chipComp != null && chipComp.Props.allocationCost > 0f)
                {
                    totalCost += chipComp.Props.allocationCost;
                    slotsWithCost.Add((slot, chipComp.Props.allocationCost));
                }
            }

            // 2. 检查是否足够（原子性检查）
            if (trion.Available < totalCost)
            {
                // 改为普通日志，避免红色报错
                return false;
            }

            // 3. 设置战斗体激活标志（必须在Allocate之前设置，因为某些检查依赖此标志）
            IsCombatBodyActive = true;

            // 4. 逐个Allocate（此时已确保足够，理论上不会失败）
            float totalAllocated = 0f;
            int successCount = 0;

            foreach (var (slot, cost) in slotsWithCost)
            {
                bool ok = trion.Allocate(cost);
                if (ok)
                {
                    totalAllocated += cost;
                    successCount++;
                }
                else
                {
                    // 理论上不应该走到这里（因为已经预检查过）
                    Log.Error($"[BDP] Allocate失败（异常情况）: {slot.loadedChip.def.defName} cost={cost:F1} available={trion.Available:F1}");
                }
            }


            // 5. 返回成功
            return true;
        }

        /// <summary>
        /// 开始战斗体激活流程（v2.2重构版 - 已废弃，保留用于兼容性）。
        ///
        /// ⚠️ 此方法已被拆分为TryAllocateTrionForCombatBody + ActivateAllSpecial。
        /// 新代码应使用拆分后的方法以保证原子性。
        ///
        /// 由Gene_TrionGland在战斗体激活时调用。
        /// 流程：设置标志 → 逐个芯片Allocate → 激活特殊槽。
        /// </summary>
        [System.Obsolete("已废弃，请使用TryAllocateTrionForCombatBody + ActivateAllSpecial")]
        public void BeginCombatBodyActivation()
        {
            Log.Message($"[BDP] BeginCombatBodyActivation() 被调用（已废弃）");

            // 1. 设置战斗体激活标志
            IsCombatBodyActive = true;

            // 2. 遍历所有芯片，逐个Allocate（锁定Trion占用）
            var trion = TrionComp;
            float totalAllocated = 0f;
            foreach (var slot in AllSlots())
            {
                if (slot.loadedChip == null) continue;
                var chipComp = slot.loadedChip.TryGetComp<TriggerChipComp>();
                if (chipComp != null && chipComp.Props.allocationCost > 0f)
                {
                    bool ok = trion?.Allocate(chipComp.Props.allocationCost) ?? false;
                    if (ok)
                    {
                        totalAllocated += chipComp.Props.allocationCost;
                    }
                    else
                    {
                        Log.Warning($"[BDP] Allocate失败: {slot.loadedChip.def.defName} cost={chipComp.Props.allocationCost} available={trion?.Available ?? 0f:F1}");
                    }
                }
            }

            // 3. 激活特殊槽芯片
            ActivateAllSpecial();

            Log.Message($"[BDP] 战斗体激活完成 (allocated={totalAllocated:F1}, trion={trion?.Cur:F1}/{trion?.Max:F1})");
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
        /// <param name="pawnOverride">显式指定Pawn（用于Notify_Unequipped中，此时OwnerPawn可能已为null）</param>
        public void DismissCombatBody(Pawn pawnOverride = null)
        {
            DeactivateAll(pawnOverride);
            // 清除所有槽位的禁用标志（战斗体解除 → 部位恢复 → 槽位可用）
            foreach (var slot in AllSlots())
                slot.isDisabled = false;
            var trion = (pawnOverride ?? OwnerPawn)?.GetComp<CompTrion>();
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

        // 静态构造函数：订阅部位破坏事件（v12.2新增：手部缺失联动）
        static CompTriggerBody()
        {
            BDPEvents.OnPartDestroyed += OnPartDestroyed;
        }

        /// <summary>
        /// 响应部位破坏事件（静态事件处理器）。
        /// </summary>
        private static void OnPartDestroyed(PartDestroyedEventArgs args)
        {
            if (!args.IsHandPart) return;

            // 找到该Pawn装备的触发体
            CompTriggerBody comp = args.Pawn.equipment?.Primary?.GetComp<CompTriggerBody>();
            if (comp != null)
            {
                comp.OnHandDestroyed(args.HandSide);
            }
        }

        // ═══════════════════════════════════════════
        //  存档
        // ═══════════════════════════════════════════

        // PostSpawnSetup、Notify_Equipped、Notify_Unequipped、PostDestroy、PostExposeData
        // 已移至 CompTriggerBody.Lifecycle.cs

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
                        secondaryVerb = leftHandSecondaryVerb, // v9.0：副攻击（可以是齐射或其他模式）
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
                        secondaryVerb = rightHandSecondaryVerb, // v9.0：副攻击（可以是齐射或其他模式）
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
                        secondaryVerb = dualSecondaryVerb, // v9.0：副攻击（可以是齐射或其他模式）
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
                        secondaryVerb = comboSecondaryVerb, // v9.0：副攻击
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

            if (!DebugSettings.godMode) yield break;
            foreach (var g in GetDebugGizmos()) yield return g;
        }

        // ═══════════════════════════════════════════
        //  组合能力系统（v4.0 F1新增）
        // ═══════════════════════════════════════════

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

        // ═══════════════════════════════════════════
        //  ICombatBodySupport接口实现（v11.0战斗体系统重构）
        // ═══════════════════════════════════════════

        /// <summary>
        /// 实现ICombatBodySupport.TryAllocateForCombatBody。
        /// 委托给现有的TryAllocateTrionForCombatBody方法。
        /// </summary>
        bool ICombatBodySupport.TryAllocateForCombatBody()
        {
            return TryAllocateTrionForCombatBody();
        }

        /// <summary>
        /// 实现ICombatBodySupport.ReleaseFromCombatBody。
        /// 委托给DismissCombatBody方法，执行完整的解除逻辑。
        /// </summary>
        void ICombatBodySupport.ReleaseFromCombatBody()
        {
            DismissCombatBody();
        }

        /// <summary>
        /// 实现ICombatBodySupport.ActivateSpecialSlots。
        /// 委托给现有的ActivateAllSpecial方法。
        /// </summary>
        void ICombatBodySupport.ActivateSpecialSlots()
        {
            ActivateAllSpecial();
        }

        /// <summary>
        /// 实现ICombatBodySupport.DeactivateSpecialSlots。
        /// 关闭所有特殊槽芯片。
        /// </summary>
        void ICombatBodySupport.DeactivateSpecialSlots()
        {
            if (specialSlots == null) return;
            foreach (var slot in specialSlots)
            {
                if (slot.loadedChip != null && slot.isActive)
                {
                    DeactivateSlot(slot);
                }
            }
        }

        // ═══════════════════════════════════════════
        //  手部缺失联动（v12.2新增）
        // ═══════════════════════════════════════════

        /// <summary>
        /// 查询指定侧是否被禁用（手部/手臂被毁）。
        /// 只要该侧有任一槽位被禁用即返回true。
        /// </summary>
        public bool IsSideDisabled(SlotSide side)
        {
            var slots = side == SlotSide.LeftHand ? leftHandSlots
                      : side == SlotSide.RightHand ? rightHandSlots
                      : specialSlots;
            if (slots == null) return false;
            for (int i = 0; i < slots.Count; i++)
                if (slots[i].isDisabled) return true;
            return false;
        }

        /// <summary>
        /// 处理手部破坏（实例方法）。
        /// </summary>
        private void OnHandDestroyed(HandSide side)
        {
            Log.Message($"[BDP] 检测到{(side == HandSide.Left ? "左手" : "右手")}被破坏，强制关闭对应槽位");

            if (side == HandSide.Left)
            {
                ForceDeactivateLeftSlots("左手缺失");
            }
            else
            {
                ForceDeactivateRightSlots("右手缺失");
            }
        }

        /// <summary>
        /// 强制禁用左手槽位（手部/手臂被毁时调用）。
        /// 芯片保留在槽位，但标记为禁用，激活的芯片被关闭。
        /// </summary>
        private void ForceDeactivateLeftSlots(string reason)
        {
            if (leftHandSlots == null) return;

            // 获取装备者（使用CompEquippable的Holder属性）
            Pawn pawn = Holder;
            if (pawn == null)
            {
                Log.Warning("[BDP] ForceDeactivateLeftSlots: 无法获取装备者Pawn");
                return;
            }

            foreach (var slot in leftHandSlots)
            {
                // 关闭激活的芯片（但不清除loadedChip）
                if (slot.isActive)
                {
                    slot.isActive = false;
                    Log.Message($"[BDP] 左手槽位[{slot.index}]芯片关闭: {slot.loadedChip?.Label ?? "null"} 原因={reason}");
                }
                // 标记槽位为禁用
                slot.isDisabled = true;
            }

            // 清理左手Verb
            ClearSideVerbs(SlotSide.LeftHand);
            RebuildVerbs(pawn);
        }

        /// <summary>
        /// 强制禁用右手槽位（手部/手臂被毁时调用）。
        /// 芯片保留在槽位，但标记为禁用，激活的芯片被关闭。
        /// </summary>
        private void ForceDeactivateRightSlots(string reason)
        {
            if (rightHandSlots == null) return;

            // 获取装备者（使用CompEquippable的Holder属性）
            Pawn pawn = Holder;
            if (pawn == null)
            {
                Log.Warning("[BDP] ForceDeactivateRightSlots: 无法获取装备者Pawn");
                return;
            }

            foreach (var slot in rightHandSlots)
            {
                // 关闭激活的芯片（但不清除loadedChip）
                if (slot.isActive)
                {
                    slot.isActive = false;
                    Log.Message($"[BDP] 右手槽位[{slot.index}]芯片关闭: {slot.loadedChip?.Label ?? "null"} 原因={reason}");
                }
                // 标记槽位为禁用
                slot.isDisabled = true;
            }

            // 清理右手Verb
            ClearSideVerbs(SlotSide.RightHand);
            RebuildVerbs(pawn);
        }

        // ── 调试/开发工具方法已提取到 CompTriggerBody.Debug.cs（Fix-8：partial class） ──
    }
}
