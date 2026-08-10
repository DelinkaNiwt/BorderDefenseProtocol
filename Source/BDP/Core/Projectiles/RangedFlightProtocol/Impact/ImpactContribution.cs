using System.Collections.Generic;
using BDP.Core.AttackExecution.RangedModules.Runtime;
using BDP.Core.Projectiles.RangedFlightProtocol.Model;

namespace BDP.Core.Projectiles.RangedFlightProtocol.Impact
{
    /// <summary>
    /// Impact 阶段模块贡献。
    /// 它只允许模块生成 DamagePlan / AreaEffectPlan 等正式计划。
    /// </summary>
    public sealed class ImpactContribution
    {
        /// <summary>
        /// 当前模块提交的统一停止请求。
        /// </summary>
        public RangedStageStopRequest Stop { get; } = new RangedStageStopRequest();

        /// <summary>
        /// 当前模块是否抑制原版基线命中层。
        /// </summary>
        public bool SuppressBaselineImpact { get; set; }

        /// <summary>
        /// 当前模块是否提交了直接伤害计划覆盖。
        /// </summary>
        public bool HasDirectDamage { get; set; }

        /// <summary>
        /// 当前模块提交的直接伤害计划覆盖。
        /// </summary>
        public DamagePlan OverrideDirectDamage { get; set; }

        /// <summary>
        /// 当前模块是否提交了区域效果计划覆盖。
        /// </summary>
        public bool HasAreaEffect { get; set; }

        /// <summary>
        /// 当前模块提交的区域效果计划覆盖。
        /// </summary>
        public AreaEffectPlan OverrideAreaEffect { get; set; }

        /// <summary>
        /// 当前模块要追加的额外直接伤害计划集合。
        /// </summary>
        public List<DamagePlan> ExtraDamagesToAppend { get; } = new List<DamagePlan>();

        /// <summary>
        /// 当前模块要追加到 Impact 阶段结果中的标签。
        /// </summary>
        public List<string> TagsToAppend { get; } = new List<string>();
    }
}
