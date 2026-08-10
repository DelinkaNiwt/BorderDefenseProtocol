namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 攻击效果追踪信息承载接口。
    /// 它只服务日志追踪，不参与攻击语义判断。
    /// </summary>
    internal interface IAttackEffectTraceCarrier
    {
        /// <summary>
        /// 当前效果所属的攻击实例标识。
        /// </summary>
        string AttackInstanceId { get; set; }

        /// <summary>
        /// 当前效果对应的正式结果标识。
        /// </summary>
        string ResultId { get; set; }
    }
}
