using System;
using System.Collections.Generic;
using System.Linq;
using BDP.Core.Chips;
using BDP.Core.Requirements;
using BDP.Support.Diagnostics;
using RimWorld;
using Verse;

namespace BDP.Core.Trigger
{
    /// <summary>Trigger 切换请求与规则服务，保留入口、装载规则和排斥校验。</summary>
    internal sealed class TriggerService
    {
        /// <summary>
        /// 芯片启用延迟默认值。
        /// 1 秒 = 60 ticks。
        /// </summary>
        public const int DefaultChipActivationDelayTicks = 60;

        /// <summary>
        /// 芯片停用延迟默认值。
        /// 0.5 秒 = 30 ticks。
        /// </summary>
        public const int DefaultChipDeactivationDelayTicks = 30;

        /// <summary>处理单侧启用请求，只负责入口校验、规则判定和切换提交。</summary>
        public bool RequestActivate(TriggerSwitchContext context, TriggerSide side, int slotIndex)
        {
            if (context == null)
            {
                return false;
            }

            TriggerSlotState nextSlot = TriggerSwitchTransitionService.NormalizeDirectControlSlot(
                context.GetSlot != null ? context.GetSlot(side, slotIndex) : null,
                context.GetSlot);
            if (nextSlot == null || nextSlot.LoadedChip == null || nextSlot.IsDisabled)
            {
                BdpDiagnostics.Throttled("trigger.activate.reject." + side + "." + slotIndex, "Activation rejected: slot missing, empty, or disabled.", 30);
                return false;
            }

            if (nextSlot.IsActive)
            {
                return true;
            }

            if (TriggerSwitchTransitionService.IsSamePendingTarget(
                nextSlot,
                context.GetSwitchContext,
                context.GetSlot))
            {
                return true;
            }

            PawnRequirementCheckResult requirementResult =
                context.EvaluateActivationRequirements != null
                    ? context.EvaluateActivationRequirements(nextSlot.LoadedChip)
                    : null;
            if (requirementResult != null && !requirementResult.Satisfied)
            {
                Messages.Message(
                    BuildActivationRequirementRejection(nextSlot.LoadedChip, requirementResult),
                    MessageTypeDefOf.RejectInput,
                    false);
                return false;
            }

            TriggerSwitchTransitionService.CancelConflictingPendingTargets(
                nextSlot,
                context.GetSwitchContext,
                context.SetSwitchContext,
                context.GetSlot,
                context.HasActivationExclusionConflict);

            IReadOnlyList<TriggerSlotState> blockers = context.FindActivationBlockers != null
                ? context.FindActivationBlockers(nextSlot)
                : new List<TriggerSlotState>();
            return TriggerSwitchTransitionService.RequestActivate(
                nextSlot,
                context.GetSlot,
                context.GetActiveSlot,
                context.GetActiveSlotRaw,
                blockers,
                context.FindActivationBlockers,
                context.IsPendingTargetValid,
                context.GetSwitchContext,
                context.SetSwitchContext,
                context.ResolveChipActivationDelayTicks,
                context.ResolveChipDeactivationDelayTicks,
                context.CurrentTick,
                context.NotifySlotActivationCommitted,
                context.NotifySlotDeactivated);
        }

        /// <summary>
        /// 把全部失败条件按 XML 顺序合并成一次玩家提示。
        /// </summary>
        private static string BuildActivationRequirementRejection(
            Thing chip,
            PawnRequirementCheckResult requirementResult)
        {
            string message = "BDP_Message_Chip_ActivationFailure".Translate(
                chip != null ? chip.LabelShortCap : "BDP_Message_Chip_Default".Translate().ToString());
            IReadOnlyList<PawnRequirementSnapshot> failures =
                requirementResult != null ? requirementResult.Failures : null;
            if (failures == null || failures.Count == 0)
            {
                return message;
            }

            for (int i = 0; i < failures.Count; i++)
            {
                PawnRequirementSnapshot failure = failures[i];
                if (failure != null)
                {
                    message += "\n- " + failure.FailureReason;
                }
            }

            return message;
        }

        /// <summary>处理单侧停用请求，只负责入口路由。</summary>
        public bool RequestDeactivate(TriggerSwitchContext context, TriggerSide side)
        {
            if (context == null)
            {
                return false;
            }

            return TriggerSwitchTransitionService.RequestDeactivate(
                side,
                context.GetActiveSlot,
                context.GetActiveSlotRaw,
                context.GetSlot,
                context.GetSwitchContext,
                context.SetSwitchContext,
                context.ResolveChipActivationDelayTicks,
                context.ResolveChipDeactivationDelayTicks,
                context.CurrentTick,
                context.NotifySlotActivationCommitted,
                context.NotifySlotDeactivated);
        }

        /// <summary>读取芯片的正式装载声明。</summary>
        public ChipLoadoutContract GetChipLoadout(Thing chip)
        {
            ChipDefinitionReadResult readResult = ChipSurfaceAccess.Read(chip);
            if (readResult == null
                || readResult.Validation == null
                || !readResult.Validation.IsValid
                || readResult.Contract == null)
            {
                return null;
            }

            return readResult.Contract.Loadout;
        }

        /// <summary>读取芯片的正式激活音效声明。</summary>
        public ChipActivationAudioContract GetChipActivationAudio(Thing chip)
        {
            ChipLoadoutContract loadout = GetChipLoadout(chip);
            return loadout != null ? loadout.ActivationAudio : null;
        }

        /// <summary>解析某枚芯片的启用延迟。</summary>
        public int ResolveChipActivationDelayTicks(Thing chip)
        {
            ChipLoadoutContract loadout = GetChipLoadout(chip);
            return loadout != null && loadout.ActivationDelayTicks >= 0
                ? loadout.ActivationDelayTicks
                : DefaultChipActivationDelayTicks;
        }

        /// <summary>解析某枚芯片的停用延迟。</summary>
        public int ResolveChipDeactivationDelayTicks(Thing chip)
        {
            ChipLoadoutContract loadout = GetChipLoadout(chip);
            return loadout != null && loadout.DeactivationDelayTicks >= 0
                ? loadout.DeactivationDelayTicks
                : DefaultChipDeactivationDelayTicks;
        }

        /// <summary>读取芯片的正式 Trion 声明。</summary>
        public ChipTrionContract GetChipTrionContract(Thing chip)
        {
            ChipDefinitionReadResult readResult = ChipSurfaceAccess.Read(chip);
            if (readResult == null
                || readResult.Validation == null
                || !readResult.Validation.IsValid
                || readResult.Contract == null)
            {
                return null;
            }

            return readResult.Contract.Trion;
        }

        /// <summary>
        /// 找出阻止目标立即启用的全部逻辑活动芯片。
        /// 同一控制范围的活动芯片必须让位；其它侧只在共享互斥组时让位。
        /// </summary>
        public IReadOnlyList<TriggerSlotState> FindActivationBlockers(
            TriggerSlotState targetSlot,
            IEnumerable<TriggerSlotState> activeSlots,
            Func<TriggerSide, int, TriggerSlotState> getSlot)
        {
            TriggerSlotState normalizedTarget =
                TriggerSwitchTransitionService.NormalizeDirectControlSlot(targetSlot, getSlot);
            List<TriggerSlotState> blockers = new List<TriggerSlotState>();
            HashSet<string> seenRoots = new HashSet<string>();
            foreach (TriggerSlotState activeSlot in activeSlots ?? Enumerable.Empty<TriggerSlotState>())
            {
                TriggerSlotState normalizedActive =
                    TriggerSwitchTransitionService.NormalizeDirectControlSlot(activeSlot, getSlot);
                if (normalizedTarget == null
                    || normalizedActive == null
                    || normalizedActive == normalizedTarget
                    || normalizedActive.IsBindingMirror)
                {
                    continue;
                }

                bool mustYield = TriggerSwitchTransitionService.SharesActivationControlScope(
                    normalizedTarget,
                    normalizedActive);
                if (!mustYield
                    && !HasActivationExclusionConflict(normalizedTarget, normalizedActive))
                {
                    continue;
                }

                string rootKey = normalizedActive.Side + ":" + normalizedActive.Index;
                if (seenRoots.Add(rootKey))
                {
                    blockers.Add(normalizedActive);
                }
            }

            return blockers;
        }

        /// <summary>
        /// 判断两枚逻辑芯片是否共享至少一个启用互斥组。
        /// </summary>
        public bool HasActivationExclusionConflict(
            TriggerSlotState leftSlot,
            TriggerSlotState rightSlot)
        {
            ChipLoadoutContract leftLoadout =
                GetChipLoadout(leftSlot != null ? leftSlot.LoadedChip : null);
            ChipLoadoutContract rightLoadout =
                GetChipLoadout(rightSlot != null ? rightSlot.LoadedChip : null);
            return SharesActivationExclusionGroup(
                leftLoadout != null ? leftLoadout.ActivationExclusionGroups : null,
                rightLoadout != null ? rightLoadout.ActivationExclusionGroups : null);
        }

        /// <summary>
        /// 判断两组受控定义是否共享至少一个启用互斥组。
        /// </summary>
        private static bool SharesActivationExclusionGroup(
            IReadOnlyList<ChipExclusionGroupDef> left,
            IReadOnlyList<ChipExclusionGroupDef> right)
        {
            if (left == null || left.Count == 0 || right == null || right.Count == 0)
            {
                return false;
            }

            HashSet<ChipExclusionGroupDef> leftGroups =
                new HashSet<ChipExclusionGroupDef>(left.Where(group => group != null));
            return right.Any(group => group != null && leftGroups.Contains(group));
        }

        /// <summary>解析一次芯片装载是否需要占据对侧同编号槽位。</summary>
        public bool TryResolvePairedOccupancyLoad(
            TriggerSide side,
            int slotIndex,
            Thing chip,
            Func<TriggerSide, int, TriggerSlotState> getSlot,
            out TriggerSlotState mirrorSlot,
            out string rejectCode,
            out string rejectMessage)
        {
            mirrorSlot = null;
            rejectCode = null;
            rejectMessage = null;

            ChipDefinitionReadResult chipReadResult = ChipSurfaceAccess.Read(chip);
            if (chipReadResult == null
                || chipReadResult.Contract == null
                || chipReadResult.Validation == null
                || !chipReadResult.Validation.IsValid)
            {
                rejectCode = "invalid_chip_contract";
                rejectMessage = "BDP_Message_Chip_ValidationRejected".Translate();
                return false;
            }

            ChipLoadoutContract constraint = chipReadResult.Contract.Loadout;
            if (constraint == null)
            {
                return true;
            }

            if (!IsSlotOccupancyAllowed(
                constraint.SlotRegion,
                constraint.SlotOccupancy,
                side))
            {
                rejectCode = "loadout_slot_occupancy";
                rejectMessage = "BDP_Message_Chip_SlotRestriction".Translate();
                return false;
            }

            if (constraint.SlotOccupancy != ChipSlotOccupancy.PairedHands)
            {
                return true;
            }

            if (!TriggerSwitchTransitionService.IsHandSide(side))
            {
                rejectCode = "binding_side";
                rejectMessage = "BDP_Message_Chip_PairedHandsOnly".Translate();
                return false;
            }

            mirrorSlot = TriggerSwitchTransitionService.GetOppositeIndexedSlot(side, slotIndex, getSlot);
            if (mirrorSlot == null)
            {
                rejectCode = "binding_missing_partner";
                rejectMessage = "BDP_Message_Chip_MissingPartner".Translate();
                return false;
            }

            if (mirrorSlot.LoadedChip != null)
            {
                rejectCode = "binding_occupied";
                rejectMessage = "BDP_Message_Chip_PartnerOccupied".Translate();
                return false;
            }

            return true;
        }

        /// <summary>判断目标槽位是否属于芯片声明的槽位区域。</summary>
        private static bool IsSlotRegionAllowed(ChipSlotRegion slotRegion, TriggerSide side)
        {
            switch (slotRegion)
            {
                case ChipSlotRegion.MainSub:
                    return side == TriggerSide.Main || side == TriggerSide.Sub;
                case ChipSlotRegion.Special:
                    return side == TriggerSide.Special;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 判断槽位区域、占用方式和目标侧别是否形成合法组合。
        /// 该方法是坏定义漏过上层校验时的最后一道运行边界。
        /// </summary>
        internal static bool IsSlotOccupancyAllowed(
            ChipSlotRegion slotRegion,
            ChipSlotOccupancy slotOccupancy,
            TriggerSide side)
        {
            if (!System.Enum.IsDefined(typeof(ChipSlotOccupancy), slotOccupancy)
                || slotOccupancy == ChipSlotOccupancy.Unspecified
                || !IsSlotRegionAllowed(slotRegion, side))
            {
                return false;
            }

            if (slotOccupancy == ChipSlotOccupancy.PairedHands)
            {
                return slotRegion == ChipSlotRegion.MainSub
                    && TriggerSwitchTransitionService.IsHandSide(side);
            }

            return slotOccupancy == ChipSlotOccupancy.Single;
        }
    }

    /// <summary>
    /// Trigger 切换事务的正式上下文。
    /// 负责把宿主真值读取、切换写回和事件通知收口成单一参数对象。
    /// </summary>
    internal sealed class TriggerSwitchContext
    {
        /// <summary>
        /// 读取指定侧别和索引的槽位。
        /// </summary>
        public Func<TriggerSide, int, TriggerSlotState> GetSlot;

        /// <summary>
        /// 读取指定侧别当前正式激活槽位。
        /// </summary>
        public Func<TriggerSide, TriggerSlotState> GetActiveSlot;

        /// <summary>
        /// 读取指定侧别当前原始激活槽位。
        /// </summary>
        public Func<TriggerSide, TriggerSlotState> GetActiveSlotRaw;

        /// <summary>
        /// 读取指定侧别当前保存的切换上下文。
        /// </summary>
        public Func<TriggerSide, SwitchContext> GetSwitchContext;

        /// <summary>
        /// 写回指定侧别的切换上下文。
        /// </summary>
        public Action<TriggerSide, SwitchContext> SetSwitchContext;

        /// <summary>
        /// 找出目标当前的全部活动阻挡者。
        /// </summary>
        public Func<TriggerSlotState, IReadOnlyList<TriggerSlotState>> FindActivationBlockers;

        /// <summary>
        /// 判断两枚逻辑芯片是否存在启用互斥。
        /// </summary>
        public Func<TriggerSlotState, TriggerSlotState, bool> HasActivationExclusionConflict;

        /// <summary>
        /// 判断等待或开启中的目标是否仍然合法。
        /// </summary>
        public Func<TriggerSlotState, bool> IsPendingTargetValid;

        /// <summary>
        /// 对目标芯片执行一次完整激活条件求值。
        /// </summary>
        public Func<Thing, PawnRequirementCheckResult> EvaluateActivationRequirements;

        /// <summary>
        /// 解析某枚芯片启用延迟的函数。
        /// </summary>
        public Func<Thing, int> ResolveChipActivationDelayTicks;

        /// <summary>
        /// 解析某枚芯片停用延迟的函数。
        /// </summary>
        public Func<Thing, int> ResolveChipDeactivationDelayTicks;

        /// <summary>
        /// 当前游戏刻。
        /// </summary>
        public int CurrentTick;

        /// <summary>
        /// 正式提交激活时的通知回调。
        /// </summary>
        public Action<TriggerSide, int, Thing> NotifySlotActivationCommitted;

        /// <summary>
        /// 正式提交停用时的通知回调。
        /// </summary>
        public Action<TriggerSide, int, Thing> NotifySlotDeactivated;
    }
}
