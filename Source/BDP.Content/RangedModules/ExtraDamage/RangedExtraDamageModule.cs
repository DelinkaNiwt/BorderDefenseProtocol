using BDP.Core.AttackExecution;
using BDP.Core.Projectiles.RangedFlightProtocol.Impact;
using BDP.Core.Projectiles.RangedFlightProtocol.Model;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Content.RangedModules.ExtraDamage
{
    /// <summary>
    /// 通用远程额外伤害模块。
    /// 它只生成额外 DamagePlan（伤害计划），不直接调用原版伤害入口。
    /// </summary>
    public sealed class RangedExtraDamageModule :
        IRangedAttackModuleRuntime,
        IImpactStageModule
    {
        /// <summary>
        /// 当前模块绑定的冻结配置。
        /// </summary>
        private RangedExtraDamageConfig config;

        /// <summary>
        /// 初始化当前模块运行时。
        /// </summary>
        void IRangedAttackModuleRuntime.Initialize(RangedAttackModuleRuntimeContext context)
        {
            config = context != null && context.Config is RangedExtraDamageConfig typedConfig
                ? typedConfig.CloneTyped()
                : new RangedExtraDamageConfig();
        }

        /// <summary>
        /// 在有效命中后追加一条独立伤害计划。
        /// </summary>
        void IImpactStageModule.Contribute(
            in ImpactStageContext context,
            ImpactContribution contribution)
        {
            if (contribution == null
                || config == null
                || !context.IsValidHit
                || context.Projectile == null
                || context.HitThing == null
                || (config.OnlyPawns && !(context.HitThing is Pawn)))
            {
                return;
            }

            bool resolveAmountAfterPrimaryImpact =
                config.AmountMode == RangedExtraDamageAmountMode.HitPartMaxHealthPercent;
            float amount = resolveAmountAfterPrimaryImpact ? 0f : ResolveImmediateAmount(context);
            if (!resolveAmountAfterPrimaryImpact && amount <= 0f)
            {
                return;
            }

            DamagePlan damagePlan = new DamagePlan
            {
                DamageDef = config.DamageDef ?? context.Projectile.DamageDef,
                Amount = amount,
                ArmorPenetration = config.ArmorPenetration >= 0f
                    ? config.ArmorPenetration
                    : context.Projectile.ArmorPenetration,
                HitPartResolutionMode = config.HitPartResolutionMode,
                Instigator = context.Launcher,
                Weapon = context.SourceThing,
                IntendedTarget = context.AimTarget,
                SemanticContext = context.SemanticContext
            };
            if (resolveAmountAfterPrimaryImpact)
            {
                damagePlan.AmountResolver = new HitPartMaxHealthPercentResolver(config.Ratio);
            }

            contribution.ExtraDamagesToAppend.Add(damagePlan);
        }

        /// <summary>
        /// 解析不依赖最终命中部件的额外伤害量。
        /// </summary>
        private float ResolveImmediateAmount(in ImpactStageContext context)
        {
            if (config.AmountMode == RangedExtraDamageAmountMode.Fixed)
            {
                return Mathf.Max(0f, config.Amount);
            }

            if (config.AmountMode != RangedExtraDamageAmountMode.BodyPartMaxHealthSumPercent)
            {
                return 0f;
            }

            Pawn target = context.HitThing as Pawn;
            if (target == null || target.health == null || target.health.hediffSet == null)
            {
                return 0f;
            }

            float bodyPartMaxHealthSum = 0f;
            foreach (BodyPartRecord part in target.health.hediffSet.GetNotMissingParts())
            {
                if (part != null && part.def != null)
                {
                    bodyPartMaxHealthSum += part.def.GetMaxHealth(target);
                }
            }

            return bodyPartMaxHealthSum * Mathf.Max(0f, config.Ratio);
        }

        /// <summary>
        /// 按实际命中部件的最大血量解析额外伤害量。
        /// </summary>
        private sealed class HitPartMaxHealthPercentResolver : IDamageAmountResolver
        {
            /// <summary>
            /// 当前解析器使用的伤害倍率。
            /// </summary>
            private readonly float ratio;

            /// <summary>
            /// 创建命中部件最大血量百分比解析器。
            /// </summary>
            public HitPartMaxHealthPercentResolver(float ratio)
            {
                this.ratio = ratio;
            }

            /// <summary>
            /// 根据 Core 提供的最终目标和命中部件计算伤害量。
            /// </summary>
            public float Resolve(in DamageAmountResolutionContext context)
            {
                Pawn target = context.TargetThing as Pawn;
                BodyPartRecord hitPart = context.HitPart;
                if (target == null
                    || target.health == null
                    || target.health.hediffSet == null
                    || hitPart == null
                    || hitPart.def == null)
                {
                    return 0f;
                }

                return hitPart.def.GetMaxHealth(target) * Mathf.Max(0f, ratio);
            }
        }
    }
}
