using System.Collections.Generic;
using BDP.Core.AttackExecution;
using BDP.Core.AttackExecution.RangedProtocol.Model;
using BDP.Core.Projectiles.RangedFlightProtocol.Model;
using Verse;

namespace BDP.Core.Projectiles.RangedFlightProtocol.Impact
{
    /// <summary>
    /// Impact 阶段服务。
    /// 它只负责生成正式 ImpactPlan，不直接吞下游防御总裁决。
    /// </summary>
    internal sealed class ImpactStageService
    {
        private readonly List<IImpactStageModule> modules;
        private readonly List<IRangedStageAddonModule> addons;

        public ImpactStageService(IEnumerable<IImpactStageModule> modules, IEnumerable<IRangedStageAddonModule> addons)
        {
            this.modules = modules != null ? new List<IImpactStageModule>(modules) : new List<IImpactStageModule>();
            this.addons = addons != null ? new List<IRangedStageAddonModule>(addons) : new List<IRangedStageAddonModule>();
        }

        public ImpactPlan Execute(Projectile projectile, ProjectileInitPlan initPlan, FlightRecord flight, HitRecord hit, RangedAttackModuleSession moduleSession)
        {
            ImpactPlan plan = BuildBaselinePlan(projectile, initPlan, flight);

            for (int i = 0; i < modules.Count; i++)
            {
                ImpactStageContext context = new ImpactStageContext(projectile, initPlan, flight, hit, modules[i] as IRangedAttackModuleRuntime, moduleSession);
                ImpactContribution contribution = new ImpactContribution();
                modules[i].Contribute(context, contribution);
                if (contribution.Stop.IsRequested)
                {
                    plan.SuppressBaselineImpact = true;
                    plan.DamageDisposition = MergeDamageDisposition(
                        plan.DamageDisposition,
                        DamageDisposition.SuppressBaselineImpact);
                    plan.ApplyBaselineDirectDamage = false;
                    plan.BaselineDirectDamage = null;
                    plan.ApplyBaselineAreaEffect = false;
                    plan.BaselineAreaEffect = null;
                    plan.ApplyDirectDamage = false;
                    plan.DirectDamage = null;
                    plan.ApplyAreaEffect = false;
                    plan.AreaEffect = null;
                    plan.ExtraDamages.Clear();
                    plan.ExtraEffects.Clear();
                }

                plan.SuppressBaselineImpact |= contribution.SuppressBaselineImpact;
                plan.PreserveTargetResolutionWhenDamageSuppressed |=
                    contribution.PreserveTargetResolutionWhenDamageSuppressed;
                plan.ProducesAttackTargetEvents |= contribution.ProducesAttackTargetEvents;
                if (contribution.HasHitFeedbackColor)
                {
                    plan.HasHitFeedbackColor = true;
                    plan.HitFeedbackColor = contribution.HitFeedbackColor;
                    plan.HitFeedbackTargetScope = contribution.HitFeedbackTargetScope;
                }
                if (contribution.InterceptedHitFeedback != ImpactHitFeedbackMode.None)
                {
                    plan.InterceptedHitFeedback = contribution.InterceptedHitFeedback;
                }
                if (contribution.AreaPresentationPolicyOverride != null)
                {
                    plan.AreaPresentationPolicyOverride =
                        contribution.AreaPresentationPolicyOverride.Clone();
                }
                if (contribution.SuppressBaselineImpact)
                {
                    plan.DamageDisposition = MergeDamageDisposition(
                        plan.DamageDisposition,
                        DamageDisposition.SuppressBaselineImpact);
                }

                plan.DamageDisposition = MergeDamageDisposition(
                    plan.DamageDisposition,
                    contribution.DamageDisposition);
                if (contribution.HasDirectDamage)
                {
                    plan.ApplyDirectDamage = contribution.OverrideDirectDamage != null;
                    plan.DirectDamage = contribution.OverrideDirectDamage;
                }

                if (contribution.HasAreaEffect)
                {
                    plan.ApplyAreaEffect = contribution.OverrideAreaEffect != null;
                    plan.AreaEffect = contribution.OverrideAreaEffect;
                }

                plan.ExtraDamages.AddRange(contribution.ExtraDamagesToAppend);
                plan.ExtraEffects.AddRange(contribution.ExtraEffectsToAppend);
                AppendTags(plan.Tags, contribution.TagsToAppend);
            }

            RangedStageAddonDispatcher.Execute(
                addons,
                new RangedStageAddonContext(
                    RangedStageKind.Impact,
                    initPlan != null ? initPlan.Launcher : null,
                    projectile?.Map,
                    initPlan != null ? initPlan.AttackInstanceId : null,
                    initPlan != null ? initPlan.ResultId : null,
                    initPlan != null ? initPlan.EmitSequence : -1,
                    projectile,
                    initPlan != null ? initPlan.Launcher : null,
                    initPlan != null ? initPlan.SourceThing : null,
                    initPlan != null ? initPlan.AimTarget : LocalTargetInfo.Invalid,
                    flight != null ? flight.CurrentTarget : initPlan != null ? initPlan.CurrentTarget : LocalTargetInfo.Invalid,
                    flight != null ? flight.CurrentDestination : default,
                    hit != null ? hit.HitThing : null,
                    hit != null ? hit.HitCell : default,
                    initPlan != null ? initPlan.SemanticContext : null,
                    initPlan?.AttackContextSnapshot));

            return plan;
        }

        private static ImpactPlan BuildBaselinePlan(Projectile projectile, ProjectileInitPlan initPlan, FlightRecord flight)
        {
            float damageFactor = flight != null ? flight.DamageFactor : 1f;
            float radius = projectile != null && projectile.def != null && projectile.def.projectile != null
                ? projectile.def.projectile.explosionRadius
                : 0f;
            bool hasAreaEffect = radius > 0f;
            ImpactPlan plan = new ImpactPlan
            {
                SuppressBaselineImpact = false,
                ProducesAttackTargetEvents = hasAreaEffect,
                ApplyBaselineDirectDamage = !hasAreaEffect,
                BaselineDirectDamage = hasAreaEffect
                    ? null
                    : new DamagePlan
                    {
                        DamageDef = projectile != null ? projectile.DamageDef : null,
                        Amount = projectile != null ? projectile.DamageAmount * damageFactor : 0f,
                        ArmorPenetration = projectile != null ? projectile.ArmorPenetration : 0f,
                        Instigator = initPlan != null ? initPlan.Launcher : null,
                        Weapon = initPlan != null ? initPlan.SourceThing : null,
                        IntendedTarget = initPlan != null ? initPlan.AimTarget : LocalTargetInfo.Invalid,
                        SemanticContext = initPlan != null ? initPlan.SemanticContext : null
                    }
            };

            if (!hasAreaEffect || projectile?.def?.projectile == null)
            {
                return plan;
            }

            plan.ApplyBaselineAreaEffect = true;
            plan.BaselineAreaEffect = new AreaEffectPlan
            {
                DamageDef = projectile.DamageDef,
                Radius = radius,
                DamageAmount = projectile.DamageAmount * damageFactor,
                ArmorPenetration = projectile.ArmorPenetration,
                Center = projectile.Position,
                Instigator = initPlan != null ? initPlan.Launcher : null,
                Weapon = initPlan != null ? initPlan.SourceThing : null,
                SemanticContext = initPlan != null ? initPlan.SemanticContext : null
            };
            return plan;
        }

        private static void AppendTags(List<string> target, List<string> source)
        {
            if (target == null || source == null)
            {
                return;
            }

            for (int i = 0; i < source.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(source[i]))
                {
                    target.Add(source[i]);
                }
            }
        }

        /// <summary>
        /// 合并多个模块提交的伤害处置；更强的全量抑制优先。
        /// </summary>
        private static DamageDisposition MergeDamageDisposition(
            DamageDisposition current,
            DamageDisposition incoming)
        {
            if (incoming == DamageDisposition.SuppressAllProjectileImpact
                || current == DamageDisposition.SuppressAllProjectileImpact)
            {
                return DamageDisposition.SuppressAllProjectileImpact;
            }

            if (incoming == DamageDisposition.SuppressModuleExtraDamage
                || current == DamageDisposition.SuppressModuleExtraDamage)
            {
                return DamageDisposition.SuppressModuleExtraDamage;
            }

            if (incoming == DamageDisposition.SuppressBaselineImpact
                || current == DamageDisposition.SuppressBaselineImpact)
            {
                return DamageDisposition.SuppressBaselineImpact;
            }

            return DamageDisposition.Preserve;
        }
    }
}
