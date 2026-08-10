using System;
using System.Collections.Generic;
using BDP.Core.Chips;

namespace BDP.Core.Trigger.Runtime
{
    /// <summary>
    /// 已激活芯片条件的无状态低频筛选器。
    /// 它只决定本刻是否到期以及哪些真实根槽需要检查，不负责修改 Trigger 真值。
    /// </summary>
    internal sealed class TriggerActivationRequirementMonitor
    {
        /// <summary>正式复查间隔：每 60 游戏刻一次。</summary>
        internal const int CheckIntervalTicks = 60;

        /// <summary>共享无状态监控器。</summary>
        internal static readonly TriggerActivationRequirementMonitor Instance =
            new TriggerActivationRequirementMonitor();

        /// <summary>禁止外部创建重复监控器。</summary>
        private TriggerActivationRequirementMonitor()
        {
        }

        /// <summary>
        /// 到期时收集唯一真实激活根槽；未到期时在枚举槽位前立即返回。
        /// </summary>
        internal bool TryCollectDueActiveRoots(
            int currentTick,
            int stableThingId,
            IEnumerable<TriggerSlotState> slots,
            Func<TriggerSide, SwitchContext> getSwitchContext,
            out IReadOnlyList<TriggerSlotState> activeRoots)
        {
            int stableThingOffset = NormalizeOffset(stableThingId);
            if ((currentTick + stableThingOffset) % CheckIntervalTicks != 0)
            {
                activeRoots = null;
                return false;
            }

            List<TriggerSlotState> roots = new List<TriggerSlotState>();
            foreach (TriggerSlotState slot in slots ?? new List<TriggerSlotState>())
            {
                if (slot == null
                    || !slot.IsActive
                    || slot.IsBindingMirror
                    || slot.LoadedChip == null)
                {
                    continue;
                }

                SwitchContext switchContext =
                    getSwitchContext != null ? getSwitchContext(slot.Side) : null;
                if (switchContext != null && switchContext.phase == SwitchPhase.Deactivating)
                {
                    continue;
                }

                ChipDefinitionReadResult readResult = ChipSurfaceAccess.Read(slot.LoadedChip);
                if (readResult?.Validation == null
                    || !readResult.Validation.IsValid
                    || readResult.Contract?.ActivationRequirements == null
                    || readResult.Contract.ActivationRequirements.Count == 0)
                {
                    continue;
                }

                roots.Add(slot);
            }

            activeRoots = roots.AsReadOnly();
            return true;
        }

        /// <summary>把稳定 ThingID 数值归一到 0～59。</summary>
        private static int NormalizeOffset(int stableThingId)
        {
            int offset = stableThingId % CheckIntervalTicks;
            return offset < 0 ? offset + CheckIntervalTicks : offset;
        }
    }
}
