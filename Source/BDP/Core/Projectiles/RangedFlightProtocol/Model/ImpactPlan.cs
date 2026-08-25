using System.Collections.Generic;
using BDP.Core.Projectiles.RangedFlightProtocol.Impact;
using UnityEngine;

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
        /// 当前 Impact 的最终伤害处置方式。
        /// </summary>
        public DamageDisposition DamageDisposition { get; set; } = DamageDisposition.Preserve;

        /// <summary>
        /// 伤害被取消时是否仍保留攻击生产者的目标解析。
        /// </summary>
        public bool PreserveTargetResolutionWhenDamageSuppressed { get; set; }

        /// <summary>
        /// 当前最终攻击计划是否存在攻击目标生产者。
        /// </summary>
        public bool ProducesAttackTargetEvents { get; set; }

        /// <summary>
        /// 当前最终攻击计划是否带有可选命中反馈颜色。
        /// </summary>
        public bool HasHitFeedbackColor { get; set; }

        /// <summary>
        /// 当前最终攻击计划的命中反馈颜色。
        /// </summary>
        public Color HitFeedbackColor { get; set; } = Color.white;

        /// <summary>
        /// 命中反馈颜色订阅的目标范围。
        /// </summary>
        public ExtraEffectTargetScope HitFeedbackTargetScope { get; set; } = ExtraEffectTargetScope.DirectHitThing;

        /// <summary>
        /// 伤害被模块拦截后是否补回原版 Pawn 受击反馈。
        /// </summary>
        public ImpactHitFeedbackMode InterceptedHitFeedback { get; set; } = ImpactHitFeedbackMode.None;

        /// <summary>
        /// 当前范围生产者最终使用的表现策略覆盖。
        /// </summary>
        public ExplosionPresentationPolicy AreaPresentationPolicyOverride { get; set; }

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
        /// 当前 Impact 最终要执行的独立额外效果计划集合。
        /// </summary>
        public List<ExtraEffectPlan> ExtraEffects { get; set; } = new List<ExtraEffectPlan>();

        /// <summary>
        /// Impact 阶段最终附带的标签集合。
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();
    }
}
