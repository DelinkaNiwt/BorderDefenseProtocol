namespace BDP.Core.Trigger
{
    /// <summary>
    /// Trigger 正式动作若被提交，预期会经过的过渡模式。
    /// </summary>
    public enum TriggerInteractionTransitionMode
    {
        // 当前没有有效过渡模式。
        None,
        // 当前动作可立即生效，不进入切换过程。
        Immediate,
        // 当前动作会进入单侧局部切换。
        SingleSideSwitch,
        // 当前动作会进入主副双侧同步切换。
        SynchronizedHandsSwitch
    }
}
