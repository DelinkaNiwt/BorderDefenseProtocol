namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 执行组的正式落地方式。
    /// 它只回答“这一组通过哪条运行时边界生效”，不描述组内编排顺序。
    /// </summary>
    internal enum AttackGroupExecutionKind
    {
        /// <summary>
        /// 未声明正式落地方式。
        /// </summary>
        None = 0,

        /// <summary>
        /// 当前执行组可直接进入效果派发层。
        /// 适用于不需要原版 Verb 会话承接的即时效果组。
        /// </summary>
        DirectEffect = 1,

        /// <summary>
        /// 当前执行组必须进入 Verb 会话层。
        /// 适用于需要 warmup、burst、cooldown 与持续攻击会话的远程组。
        /// </summary>
        VerbSession = 2
    }
}
