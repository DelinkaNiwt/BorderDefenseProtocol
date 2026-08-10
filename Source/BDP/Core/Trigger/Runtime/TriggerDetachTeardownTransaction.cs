using System;
using System.Collections.Generic;
using BDP.Core.Trigger;
using Verse;

namespace BDP.Core.Trigger.Runtime
{
    /// <summary>
    /// Trigger 脱离主装备时的收尾事务。
    /// 它只编排跨 owner 的清理顺序，不持有 Trigger 真值。
    /// </summary>
    internal sealed class TriggerDetachTeardownTransaction
    {
        /// <summary>
        /// Trigger 到 Trion 的绑定服务。
        /// </summary>
        private readonly TriggerTrionBindingService triggerTrionBindingService;

        /// <summary>
        /// 用指定绑定服务构造拆卸收尾事务。
        /// </summary>
        public TriggerDetachTeardownTransaction(TriggerTrionBindingService triggerTrionBindingService)
        {
            this.triggerTrionBindingService = triggerTrionBindingService ?? new TriggerTrionBindingService();
        }

        /// <summary>
        /// 执行 Trigger 脱离主装备后的强制收尾。
        /// </summary>
        internal void Execute(
            Pawn pawn,
            IEnumerable<TriggerSlotState> slots,
            TriggerRuntimeCoordinator runtimeCoordinator,
            Action<TriggerSide, SwitchContext> setSwitchContext)
        {
            setSwitchContext?.Invoke(TriggerSide.Main, null);
            setSwitchContext?.Invoke(TriggerSide.Sub, null);
            setSwitchContext?.Invoke(TriggerSide.Special, null);

            if (slots != null)
            {
                foreach (TriggerSlotState slot in slots)
                {
                    if (slot == null)
                    {
                        continue;
                    }

                    slot.SetActive(false);
                }
            }

            triggerTrionBindingService.UnregisterCombatBodyMaintenanceDrain(pawn);
            runtimeCoordinator?.ClearPublishedProjection(pawn);
        }
    }
}
