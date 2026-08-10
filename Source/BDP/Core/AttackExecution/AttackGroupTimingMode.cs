namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 执行组内部的时机模式。
    /// 它只描述组内动作如何被调度，不承担跨组推进职责。
    /// </summary>
    internal enum AttackGroupTimingMode
    {
        /// <summary>
        /// 未声明具体模式。
        /// </summary>
        None = 0,

        /// <summary>
        /// 当前组内所有步骤应在同一执行窗口内一起派发。
        /// </summary>
        ImmediateTogether = 1,

        /// <summary>
        /// 当前组内步骤按既定顺序逐个派发。
        /// </summary>
        SequenceInsideGroup = 2
    }
}
