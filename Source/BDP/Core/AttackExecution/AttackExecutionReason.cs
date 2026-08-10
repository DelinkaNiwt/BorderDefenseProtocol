namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 攻击执行请求的来源原因。
    /// 它只回答“这单是从哪条正式入口来的”，不承担执行分流职责。
    /// </summary>
    public enum AttackExecutionReason
    {
        /// <summary>
        /// 手动按钮发起的执行请求。
        /// </summary>
        Manual = 0,

        /// <summary>
        /// 原版自动远程链发起的执行请求。
        /// </summary>
        AutoRanged = 1,

        /// <summary>
        /// 原版自动近战链发起的执行请求。
        /// </summary>
        AutoMelee = 2
    }
}
