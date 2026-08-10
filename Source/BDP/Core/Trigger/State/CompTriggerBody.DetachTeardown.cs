using BDP.Core.AttackExecution;
using BDP.Core.Trigger.Runtime;
using BDP.Core.Trion;
using Verse;

namespace BDP.Core.Trigger
{
    /// <summary>
    /// Trigger 脱离当前主装备后的强制收尾。
    /// 这条路径直接归零 owner 真值，不沿用普通停用的延迟语义。
    /// </summary>
    public sealed partial class CompTriggerBody
    {
        /// <summary>
        /// 当 Trigger 不再是 Pawn 当前主装备时，立刻清空内部激活真值与外围发布状态。
        /// </summary>
        private void ForceTeardownOnDetach(Pawn pawn)
        {
            EnsureInternalState();
            runtimeServices.TriggerDetachTeardownTransaction.Execute(
                pawn,
                EnumerateRawSlots(),
                runtimeCoordinator,
                SetSwitchContext);
        }
    }
}
