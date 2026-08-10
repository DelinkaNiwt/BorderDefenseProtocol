using System.Collections.Generic;
using BDP.Core.AttackExecution.RangedModules.Runtime;
using Verse;

namespace BDP.Core.Projectiles.RangedFlightProtocol.Hit
{
    /// <summary>
    /// Hit 阶段模块贡献。
    /// 它只影响命中对象与命中方式，不生成伤害计划。
    /// </summary>
    public sealed class HitContribution
    {
        /// <summary>
        /// 当前模块提交的统一停止请求。
        /// </summary>
        public RangedStageStopRequest Stop { get; } = new RangedStageStopRequest();

        /// <summary>
        /// 当前局部贡献是否显式覆盖命中 Thing。
        /// </summary>
        public bool HasOverrideHitThing { get; set; }

        /// <summary>
        /// 当前局部贡献给出的命中 Thing 覆盖值。
        /// </summary>
        public Thing OverrideHitThing { get; set; }

        /// <summary>
        /// 当前局部贡献是否显式覆盖命中格。
        /// </summary>
        public bool HasOverrideHitCell { get; set; }

        /// <summary>
        /// 当前局部贡献给出的命中格覆盖值。
        /// </summary>
        public IntVec3 OverrideHitCell { get; set; }

        /// <summary>
        /// 当前局部贡献是否强制把本次命中视为地面命中。
        /// </summary>
        public bool ForceGround { get; set; }

        /// <summary>
        /// 当前局部贡献附带的标签集合。
        /// </summary>
        public List<string> TagsToAppend { get; } = new List<string>();
    }
}
