using System.Collections.Generic;

namespace BDP.Core.Projectiles.RangedFlightProtocol.Model
{
    /// <summary>
    /// 命中后进入原版宿主前的正式效果计划。
    /// 它明确区分原版基线命中层与模块提交的中性计划层。
    /// </summary>
    internal sealed class ImpactPlan
    {
        /// <summary>
        /// 是否抑制原版基线命中层。
        /// 它只影响基线层，不影响模块提交的计划层。
        /// </summary>
        public bool SuppressBaselineImpact { get; set; }

        /// <summary>
        /// 原版基线层是否应执行单体伤害。
        /// </summary>
        public bool ApplyBaselineDirectDamage { get; set; }

        /// <summary>
        /// 原版基线层的单体伤害计划。
        /// </summary>
        public DamagePlan BaselineDirectDamage { get; set; }

        /// <summary>
        /// 原版基线层是否应执行范围效果。
        /// </summary>
        public bool ApplyBaselineAreaEffect { get; set; }

        /// <summary>
        /// 原版基线层的范围效果计划。
        /// </summary>
        public AreaEffectPlan BaselineAreaEffect { get; set; }

        /// <summary>
        /// 模块计划层是否应执行单体伤害。
        /// </summary>
        public bool ApplyDirectDamage { get; set; }

        /// <summary>
        /// 模块计划层的单体伤害计划。
        /// </summary>
        public DamagePlan DirectDamage { get; set; }

        /// <summary>
        /// 模块计划层是否应执行范围效果。
        /// </summary>
        public bool ApplyAreaEffect { get; set; }

        /// <summary>
        /// 模块计划层的范围效果计划。
        /// </summary>
        public AreaEffectPlan AreaEffect { get; set; }

        /// <summary>
        /// 模块计划层附加的额外直接伤害计划。
        /// </summary>
        public List<DamagePlan> ExtraDamages { get; set; } = new List<DamagePlan>();

        /// <summary>
        /// Impact 阶段最终附带的标签集合。
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();
    }
}
