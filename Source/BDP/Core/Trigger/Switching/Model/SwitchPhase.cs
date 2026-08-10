namespace BDP.Core.Trigger
{
    /// <summary>
    /// 按侧切换状态机的最小阶段定义。
    /// </summary>
    public enum SwitchPhase
    {
        // 当前没有切换表现。
        Idle,
        // 当前处于旧槽位停用延迟。
        Deactivating,
        // 当前处于新槽位启用延迟。
        Activating,
        // 当前目标正在等待全部冲突芯片正式关闭。
        WaitingForConflicts
    }
}
