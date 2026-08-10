using System.Collections.Generic;
using BDP.Core.AttackExecution.RangedModules.Runtime;
using Verse;

namespace BDP.Core.AttackExecution.RangedProtocol.Aim
{
    /// <summary>
    /// Aim 阶段模块贡献。
    /// 它只允许模块影响瞄准阶段协议位，不允许越权改 Fire 或 Impact。
    /// </summary>
    public sealed class AimContribution
    {
        /// <summary>
        /// 当前模块提交的统一停止请求。
        /// </summary>
        public RangedStageStopRequest Stop { get; } = new RangedStageStopRequest();

        /// <summary>
        /// 当前模块是否提交了最终目标覆盖。
        /// </summary>
        public bool HasOverrideFinalTarget { get; set; }

        /// <summary>
        /// 当前模块提交的最终目标覆盖值。
        /// </summary>
        public LocalTargetInfo OverrideFinalTarget { get; set; }

        /// <summary>
        /// 当前模块乘到基线精度上的倍率。
        /// </summary>
        public float AccuracyFactorMultiplier { get; set; } = 1f;

        /// <summary>
        /// 当前模块是否提交了强制失准半径候选值。
        /// </summary>
        public bool HasForcedMissRadiusCandidate { get; set; }

        /// <summary>
        /// 当前模块提交的强制失准半径候选值。
        /// </summary>
        public float ForcedMissRadiusCandidate { get; set; }

        /// <summary>
        /// 当前模块要追加到 Aim 阶段结果中的标签。
        /// </summary>
        public List<string> TagsToAppend { get; } = new List<string>();
    }
}
