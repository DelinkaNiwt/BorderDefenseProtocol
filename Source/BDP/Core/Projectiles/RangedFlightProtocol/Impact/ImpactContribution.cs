using System.Collections.Generic;
using BDP.Core.AttackExecution.RangedModules.Runtime;
using BDP.Core.Projectiles.RangedFlightProtocol.Model;
using UnityEngine;

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
        /// 当前模块提交的伤害处置方式。
        /// </summary>
        public DamageDisposition DamageDisposition { get; set; } = DamageDisposition.Preserve;

        /// <summary>
        /// 伤害被取消时是否仍保留攻击生产者的目标解析。
        /// </summary>
        public bool PreserveTargetResolutionWhenDamageSuppressed { get; set; }

        /// <summary>
        /// 当前模块是否接管并产生攻击目标事件。
        /// </summary>
        public bool ProducesAttackTargetEvents { get; set; }

        /// <summary>
        /// 当前模块是否提交可选的命中反馈颜色。
        /// 颜色只改变原版受击闪烁表现，不改变伤害、目标或护盾语义。
        /// </summary>
        public bool HasHitFeedbackColor { get; set; }

        /// <summary>
        /// 当前模块提交的命中反馈颜色。
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
        /// 当前模块对范围生产者表现策略的可选覆盖。
        /// 它只修改已经存在的范围效果计划，不创建第二个范围生产者。
        /// </summary>
        public ExplosionPresentationPolicy AreaPresentationPolicyOverride { get; set; }

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
        /// 当前模块要追加的独立额外效果计划集合。
        /// 它与伤害计划分离，效果不以伤害存在为前提。
        /// </summary>
        public List<ExtraEffectPlan> ExtraEffectsToAppend { get; } = new List<ExtraEffectPlan>();

        /// <summary>
        /// 当前模块要追加到 Impact 阶段结果中的标签。
        /// </summary>
        public List<string> TagsToAppend { get; } = new List<string>();
    }
}
