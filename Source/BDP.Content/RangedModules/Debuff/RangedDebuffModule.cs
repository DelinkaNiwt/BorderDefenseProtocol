using System.Collections.Generic;
using BDP.Core.AttackExecution;
using BDP.Core.AttackExecution.RangedModules.Runtime;
using BDP.Core.AttackExecution.RangedProtocol.ProjectileInit;
using BDP.Core.Projectiles.Interaction;
using BDP.Core.Projectiles.RangedFlightProtocol.Impact;
using BDP.Core.Projectiles.RangedFlightProtocol.Model;
using UnityEngine;
using Verse;

namespace BDP.Content.RangedModules.Debuff
{
    /// <summary>
    /// 远程命中减益模块。
    /// 它只提交中性效果计划、伤害处置和投射物交互策略，不复制原版命中或爆炸逻辑。
    /// </summary>
    public sealed class RangedDebuffModule :
        IRangedAttackModuleRuntime,
        IProjectileInitStageModule,
        IImpactStageModule
    {
        /// <summary>
        /// 当前模块绑定的冻结配置。
        /// </summary>
        private RangedDebuffConfig config;

        /// <summary>
        /// 初始化当前模块运行时。
        /// </summary>
        void IRangedAttackModuleRuntime.Initialize(RangedAttackModuleRuntimeContext context)
        {
            config = context != null && context.Config is RangedDebuffConfig typedConfig
                ? typedConfig.CloneTyped()
                : new RangedDebuffConfig();
        }

        /// <summary>
        /// 在每枚投射物初始化时冻结护盾绕过策略。
        /// </summary>
        void IProjectileInitStageModule.Contribute(
            in ProjectileInitStageContext context,
            ProjectileInitContribution contribution)
        {
            if (contribution == null || config == null)
            {
                return;
            }

            for (int emitIndex = 0; emitIndex < context.EmitCount; emitIndex++)
            {
                contribution.PlanContributions.Add(new ProjectileInitPlanContribution
                {
                    EmitIndex = emitIndex,
                    HasInteractionPolicy = true,
                    InteractionPolicy = new ProjectileInteractionPolicy
                    {
                        BypassProjectileInterceptors = config.BypassProjectileInterceptors,
                        BypassRegisteredDamageShields = config.BypassRegisteredDamageShields
                    },
                    InitialSpeedFactorMultiplier = config.ProjectileSpeedMultiplier,
                    HasTrailColorOverride = config.HasProjectileTrailColor,
                    TrailColorOverride = config.ProjectileTrailColor,
                    HasTrailCoreOverride = config.HasProjectileTrailCore,
                    TrailCoreColorOverride = config.ProjectileTrailCoreColor,
                    TrailCoreWidthRatioOverride = config.ProjectileTrailCoreWidthRatio,
                    TrailCoreOpacityOverride = config.ProjectileTrailCoreOpacity
                });
            }
        }

        /// <summary>
        /// 在 Impact 阶段独立提交减益效果和伤害处置。
        /// </summary>
        void IImpactStageModule.Contribute(
            in ImpactStageContext context,
            ImpactContribution contribution)
        {
            if (contribution == null || context.Projectile == null || config == null)
            {
                return;
            }

            ExtraEffectPlan effectPlan = new ExtraEffectPlan
            {
                EffectKind = string.IsNullOrWhiteSpace(config.EffectKind) ? "hediff" : config.EffectKind,
                TargetScope = config.TargetScope,
                TargetCell = context.HitCell,
                Parameters = BuildParameters()
            };
            contribution.ExtraEffectsToAppend.Add(effectPlan);
            contribution.DamageDisposition = config.DamageSuppression;
            contribution.PreserveTargetResolutionWhenDamageSuppressed =
                config.PreserveTargetResolutionWhenDamageSuppressed;
            contribution.InterceptedHitFeedback = config.InterceptedHitFeedback;
            if (config.HasHitFeedbackColor)
            {
                contribution.HasHitFeedbackColor = true;
                contribution.HitFeedbackColor = config.HitFeedbackColor;
                contribution.HitFeedbackTargetScope = config.TargetScope;
            }

            bool targetsExplosion = config.TargetScope == ExtraEffectTargetScope.VanillaExplosionAffectedThings
                || config.TargetScope == ExtraEffectTargetScope.VanillaExplosionAffectedPawns;
            if (targetsExplosion
                && (config.SuppressVanillaExplosionVisualEffects
                    || config.SuppressVanillaExplosionSoundEffects
                    || config.SuppressVanillaExplosionScreenShake))
            {
                contribution.AreaPresentationPolicyOverride = new ExplosionPresentationPolicy
                {
                    SuppressVanillaVisualEffects = config.SuppressVanillaExplosionVisualEffects,
                    SuppressVanillaSoundEffects = config.SuppressVanillaExplosionSoundEffects,
                    OverrideScreenShakeFactor = config.SuppressVanillaExplosionScreenShake,
                    ScreenShakeFactor = 0f
                };
            }
        }

        /// <summary>
        /// 构造传给 Core 执行器的中性参数。
        /// </summary>
        private Dictionary<string, string> BuildParameters()
        {
            return new Dictionary<string, string>
            {
                { "hediffDef", config.HediffDef != null ? config.HediffDef.defName : null },
                { "targetFilter", config.TargetFilter.ToString() },
                { "severity", config.Severity.ToString(System.Globalization.CultureInfo.InvariantCulture) },
                { "durationTicks", config.DurationTicks.ToString(System.Globalization.CultureInfo.InvariantCulture) },
                { "stackMode", config.StackMode.ToString() }
            };
        }

    }
}
