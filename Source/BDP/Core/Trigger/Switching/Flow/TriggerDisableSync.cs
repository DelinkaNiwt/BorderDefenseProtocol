using System;
using System.Collections.Generic;
using BDP.Core.BodyConstraints;
using BDP.Support.Diagnostics;
using Verse;

namespace BDP.Core.Trigger
{
    /// <summary>
    /// Trigger 身体禁用同步器。
    /// 它只把宿主 Pawn 的正式身体事实折算回 Trigger 局部禁用真值，不持有真值。
    /// </summary>
    internal static class TriggerDisableSync
    {
        /// <summary>
        /// 按需从宿主 Pawn 的正式身体事实同步 Trigger 禁用真值。
        /// 同一宿主同一版本号下只同步一次，避免重复扫描。
        /// </summary>
        public static void SyncDisabledStateFromOwnerPawn(
            Pawn ownerPawn,
            TriggerDisableSyncCache cache,
            Func<TriggerSide, List<TriggerSlotState>> getRawSlots,
            Func<TriggerSide, SwitchContext> getSwitchContext,
            Action<TriggerSide, SwitchContext> setSwitchContext,
            Action<TriggerSlotState> deactivateBoundSlotImmediate,
            Func<TriggerSide, TriggerDisableReason> resolveExternalDisableReason,
            Action<TriggerSide, int, Thing, TriggerDisableReason> notifySlotDisableStateChanged,
            bool forceRescan)
        {
            if (cache == null)
            {
                return;
            }

            int currentVersion = PawnBodyConstraintSignalHub.GetVersion(ownerPawn);
            if (!forceRescan
                && cache.Initialized
                && ownerPawn == cache.LastSyncedPawn
                && currentVersion == cache.LastSyncedVersion)
            {
                return;
            }

            cache.LastSyncedPawn = ownerPawn;
            cache.LastSyncedVersion = currentVersion;
            cache.Initialized = true;

            TriggerDisableReason mainReason = MergeDisableReason(
                TriggerBodyDisableEvaluator.EvaluateSideDisableReason(ownerPawn, TriggerSide.Main),
                resolveExternalDisableReason != null ? resolveExternalDisableReason(TriggerSide.Main) : TriggerDisableReason.None);
            TriggerDisableReason subReason = MergeDisableReason(
                TriggerBodyDisableEvaluator.EvaluateSideDisableReason(ownerPawn, TriggerSide.Sub),
                resolveExternalDisableReason != null ? resolveExternalDisableReason(TriggerSide.Sub) : TriggerDisableReason.None);
            TriggerDisableReason specialReason = resolveExternalDisableReason != null
                ? resolveExternalDisableReason(TriggerSide.Special)
                : TriggerDisableReason.None;

            ApplySideDisableState(getRawSlots, getSwitchContext, setSwitchContext, deactivateBoundSlotImmediate, notifySlotDisableStateChanged, TriggerSide.Main, mainReason != TriggerDisableReason.None, mainReason);
            ApplySideDisableState(getRawSlots, getSwitchContext, setSwitchContext, deactivateBoundSlotImmediate, notifySlotDisableStateChanged, TriggerSide.Sub, subReason != TriggerDisableReason.None, subReason);
            ApplySideDisableState(getRawSlots, getSwitchContext, setSwitchContext, deactivateBoundSlotImmediate, notifySlotDisableStateChanged, TriggerSide.Special, specialReason != TriggerDisableReason.None, specialReason);
        }

        /// <summary>
        /// 合并身体禁用与外部禁用原因。
        /// 当前外部禁用优先级更高，因为它通常表示整套槽位都不该再被使用。
        /// </summary>
        private static TriggerDisableReason MergeDisableReason(TriggerDisableReason bodyReason, TriggerDisableReason externalReason)
        {
            return externalReason != TriggerDisableReason.None ? externalReason : bodyReason;
        }

        /// <summary>
        /// 把单侧应禁用与否同步为正式槽位真值。
        /// 若当前侧刚被禁用，还要立即切掉正式激活与本侧切换上下文。
        /// </summary>
        private static void ApplySideDisableState(
            Func<TriggerSide, List<TriggerSlotState>> getRawSlots,
            Func<TriggerSide, SwitchContext> getSwitchContext,
            Action<TriggerSide, SwitchContext> setSwitchContext,
            Action<TriggerSlotState> deactivateBoundSlotImmediate,
            Action<TriggerSide, int, Thing, TriggerDisableReason> notifySlotDisableStateChanged,
            TriggerSide side,
            bool shouldDisable,
            TriggerDisableReason reason)
        {
            List<TriggerSlotState> slots = getRawSlots(side);
            if (slots == null || slots.Count == 0)
            {
                return;
            }

            if (shouldDisable && getSwitchContext(side) != null)
            {
                setSwitchContext(side, null);
                BdpDiagnostics.Throttled("trigger.disable.cancel_switch." + side, "槽位禁用已清理当前侧切换上下文 side=" + side, 30);
            }

            for (int i = 0; i < slots.Count; i++)
            {
                TriggerSlotState slot = slots[i];
                bool stateChanged = slot.IsDisabled != shouldDisable || slot.DisabledReason != (shouldDisable ? reason : TriggerDisableReason.None);
                if (!stateChanged)
                {
                    continue;
                }

                Thing chip = slot.LoadedChip;
                if (shouldDisable && slot.IsActive)
                {
                    deactivateBoundSlotImmediate(slot);
                }

                slot.SetDisabled(shouldDisable, reason);
                notifySlotDisableStateChanged(side, slot.Index, chip, slot.DisabledReason);

                if (shouldDisable)
                {
                    BdpDiagnostics.Throttled(
                        "trigger.disable.applied." + side + "." + slot.Index,
                        "槽位禁用成立 side=" + side + ", index=" + slot.Index + ", reason=" + slot.DisabledReason,
                        30);
                }
                else
                {
                    BdpDiagnostics.Throttled(
                        "trigger.disable.cleared." + side + "." + slot.Index,
                        "槽位禁用解除 side=" + side + ", index=" + slot.Index,
                        30);
                }
            }
        }
    }
}
