using System.Collections.Generic;
using BDP.Core.Trigger;
using BDP.Core.Trion;
using Verse;

namespace BDP.Core.Trigger.Runtime
{
    /// <summary>
    /// Trigger 到 Trion 的绑定服务。
    /// 它负责预占用同步、芯片激活一次性扣费与战斗体维护账本清理，不持有 Trigger 真值本身。
    /// </summary>
    internal sealed class TriggerTrionBindingService
    {
        /// <summary>
        /// 计算当前 Trigger 已装芯片对应的预占用 Trion 总量。
        /// 这里按绑定根槽去重，避免双持镜像重复收费。
        /// </summary>
        internal float CalculateReservedTrionCost(
            IEnumerable<TriggerSlotState> slots,
            TriggerService triggerService)
        {
            float total = 0f;
            HashSet<string> chargedRoots = new HashSet<string>();
            if (slots == null || triggerService == null)
            {
                return total;
            }

            foreach (TriggerSlotState slot in slots)
            {
                if (slot == null || slot.LoadedChip == null)
                {
                    continue;
                }

                if (!chargedRoots.Add(BuildReservedChargeKey(slot)))
                {
                    continue;
                }

                var chipTrion = triggerService.GetChipTrionContract(slot.LoadedChip);
                if (chipTrion != null && chipTrion.CapacityCost > 0f)
                {
                    total += chipTrion.CapacityCost;
                }
            }

            return total;
        }

        /// <summary>
        /// 把指定 Pawn 的 Trigger 预占用 Trion 同步成给定值。
        /// </summary>
        internal void SyncReservedTrion(Pawn pawn, float reservedTrion)
        {
            ITrionCommands trionCommands = TrionSurfaceAccess.ResolveCommands(pawn);
            trionCommands?.SetReserved(reservedTrion);
        }

        /// <summary>
        /// 为刚刚正式提交激活的槽位结算一次性 Trion 成本。
        /// 若支付失败，调用方必须立刻撤销这次激活提交。
        /// </summary>
        internal bool TryCommitSlotActivation(
            Pawn pawn,
            Thing chip,
            TriggerService triggerService)
        {
            var chipTrion = triggerService != null ? triggerService.GetChipTrionContract(chip) : null;
            if (chipTrion == null)
            {
                return true;
            }

            ITrionCommands trionCommands = TrionSurfaceAccess.ResolveCommands(pawn);
            if (trionCommands == null)
            {
                return chipTrion.ActivationCost <= 0f;
            }

            if (chipTrion.ActivationCost > 0f && !trionCommands.TryConsume(chipTrion.ActivationCost))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 取消战斗体维护消耗登记。
        /// </summary>
        internal void UnregisterCombatBodyMaintenanceDrain(Pawn pawn)
        {
            TrionSurfaceAccess.ResolveCommands(pawn)?.UnregisterDrain(new TrionDrainKey("CombatBody", "Maintenance", -1, string.Empty));
        }

        /// <summary>
        /// 生成一条用于预占用去重的绑定根键。
        /// </summary>
        private static string BuildReservedChargeKey(TriggerSlotState slot)
        {
            if (slot == null)
            {
                return string.Empty;
            }

            if (slot.HasBindingPartner)
            {
                return slot.BindingRootSide + ":" + slot.BindingRootIndex;
            }

            return slot.Side + ":" + slot.Index;
        }
    }
}
