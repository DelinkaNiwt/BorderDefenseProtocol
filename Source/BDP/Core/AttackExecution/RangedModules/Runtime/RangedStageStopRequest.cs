namespace BDP.Core.AttackExecution.RangedModules.Runtime
{
    /// <summary>
    /// 远程阶段统一停止请求。
    /// 作者模块只通过它声明“这一阶段要不要停、为什么停、停到什么范围”。
    /// </summary>
    public sealed class RangedStageStopRequest
    {
        /// <summary>
        /// 当前阶段是否已经声明停止请求。
        /// </summary>
        public bool IsRequested { get; set; }

        /// <summary>
        /// 当前停止请求写回的正式原因。
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// 当前停止请求影响的范围。
        /// </summary>
        public RangedStageStopScope Scope { get; set; } = RangedStageStopScope.Stage;
    }

    /// <summary>
    /// 远程阶段停止请求的影响范围。
    /// </summary>
    public enum RangedStageStopScope
    {
        /// <summary>
        /// 只停止当前阶段的后续推进。
        /// </summary>
        Stage = 0,

        /// <summary>
        /// 停止整次攻击链的后续推进。
        /// </summary>
        Attack = 1,

        /// <summary>
        /// 停止当前投射物实例的后续推进。
        /// </summary>
        Projectile = 2
    }
}
