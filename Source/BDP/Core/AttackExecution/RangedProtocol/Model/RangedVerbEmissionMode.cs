namespace BDP.Core.AttackExecution.RangedProtocol.Model
{
    /// <summary>
    /// Verb 宿主消费当前远程动作步时的正式发射语义。
    /// 它只回答“宿主应如何消费本步计划”，不承载任何具体业务功能名词。
    /// </summary>
    internal enum RangedVerbEmissionMode
    {
        /// <summary>
        /// 当前动作步内的全部投射计划必须在同一宿主发射窗口内一起落地。
        /// </summary>
        SimultaneousStep = 0,

        /// <summary>
        /// 当前动作步按原版 burst 节奏逐次推进。
        /// 只有正式声明为顺序 burst 的动作步才能走这条路径。
        /// </summary>
        SequentialBurst = 1
    }
}
