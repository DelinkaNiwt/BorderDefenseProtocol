using System.Collections.Generic;

namespace BDP.Core.AttackExecution.RangedProtocol.Model
{
    /// <summary>
    /// Verb 宿主的一次发射窗口计划。
    /// 一个窗口对应宿主生命周期中的一次正式开火机会。
    /// </summary>
    internal sealed class RangedVerbEmissionWindowPlan
    {
        /// <summary>
        /// 当前窗口的正式发射模式。
        /// </summary>
        public RangedVerbEmissionMode EmissionMode { get; set; }

        /// <summary>
        /// 当前窗口要落地的投射物初始化计划集合。
        /// </summary>
        public IReadOnlyList<ProjectileInitPlan> ProjectilePlans { get; set; }

        /// <summary>
        /// 当前窗口按上游真值预期应落地的 emit 数量。
        /// </summary>
        public int ExpectedEmitCount { get; set; }
    }
}
