namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 目标交互推进结果种类。
    /// 它只描述主循环下一步该怎么走，不描述任何业务含义。
    /// </summary>
    public enum TargetingAdvanceKind
    {
        /// <summary>
        /// 继续收集后续输入。
        /// </summary>
        Continue = 0,

        /// <summary>
        /// 当前交互已完成，可以进入确认冻结。
        /// </summary>
        Complete = 1,

        /// <summary>
        /// 当前交互被取消。
        /// </summary>
        Cancel = 2,

        /// <summary>
        /// 当前输入被拒绝，但交互会话仍可继续。
        /// </summary>
        Reject = 3,

        /// <summary>
        /// 当前交互回退一步。
        /// </summary>
        Back = 4
    }
}
