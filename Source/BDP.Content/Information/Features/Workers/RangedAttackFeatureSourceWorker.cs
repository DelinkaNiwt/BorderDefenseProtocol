using System;
using System.Collections.Generic;
using System.Reflection;
using BDP.Core.Chips;
using BDP.Core.Expressions;
using BDP.Core.Information;
using BDP.Content.Information.Features.Sources;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Content.Information.Features.Workers
{
    /// <summary>读取远程攻击自身的射程、精度、伤害与投射物事实。</summary>
    public sealed class RangedAttackFeatureSourceWorker : IChipFeatureSourceWorker
    {
        /// <summary>读取一条远程 Verb 或远程能力表达结果。</summary>
        public IReadOnlyList<ChipFeatureValueContribution> Resolve(
            ChipFeatureSourceConfig source,
            ChipFeatureReadContext context)
        {
            List<ChipFeatureValueContribution> result =
                new List<ChipFeatureValueContribution>();
            RangedAttackFeatureSourceConfig rangedSource =
                source as RangedAttackFeatureSourceConfig;
            ChipExpressionEntryConfig entry =
                ChipFeatureSourceWorkerSupport.ResolveEntry(rangedSource, context);
            if (rangedSource == null || entry == null)
            {
                return result.AsReadOnly();
            }

            VerbProperties verbProperties =
                ChipFeatureSourceWorkerSupport.ResolveVerbProperties(entry);
            if (verbProperties == null)
            {
                return result.AsReadOnly();
            }

            ChipInformationSourceKind sourceKind =
                entry.EntryType == ChipExpressionEntryTypeConfig.Ability
                    ? ChipInformationSourceKind.Ability
                    : ChipInformationSourceKind.RangedVerb;
            if (entry.Trion != null && entry.Trion.UseCost > 0f)
            {
                ChipFeatureSourceWorkerSupport.AddSemanticNumber(
                    result, rangedSource, "BDP_Property_UseCost",
                    entry.Trion.UseCost, sourceKind);
            }
            ChipFeatureSourceWorkerSupport.AddSemanticSustainCostBySourceCount(
                result, rangedSource, entry, sourceKind);

            ChipFeatureSourceWorkerSupport.AddSemanticNumber(
                result, rangedSource, "BDP_Property_CastRange",
                verbProperties.range, sourceKind);
            if (verbProperties.minRange > 0f)
            {
                ChipFeatureSourceWorkerSupport.AddSemanticNumber(
                    result, rangedSource, "BDP_Property_MinimumCastRange",
                    verbProperties.minRange, sourceKind);
            }

            ChipFeatureSourceWorkerSupport.AddSemanticDuration(
                result, rangedSource, "BDP_Property_WarmupDuration",
                verbProperties.warmupTime, sourceKind);
            if (verbProperties.defaultCooldownTime > 0f)
            {
                ChipFeatureSourceWorkerSupport.AddSemanticDuration(
                    result, rangedSource, "BDP_Property_ActionCooldownDuration",
                    verbProperties.defaultCooldownTime, sourceKind);
            }

            AddAccuracy(result, rangedSource, verbProperties, sourceKind);
            AddBurstValues(result, rangedSource, entry, verbProperties, sourceKind);
            AddProjectileValues(result, rangedSource, entry, verbProperties.defaultProjectile, sourceKind);
            AddFlightFeatures(result, rangedSource, entry, sourceKind);

            TargetingParameters targetParams = verbProperties.targetParams;
            if (targetParams != null)
            {
                ChipFeatureSourceWorkerSupport.AddTargetTypes(
                    result, rangedSource, targetParams, sourceKind);
                ChipFeatureSourceWorkerSupport.AddSemanticBoolean(
                    result, rangedSource, "BDP_Property_RequiresLineOfSight",
                    verbProperties.requireLineOfSight, sourceKind);
            }

            return result.AsReadOnly();
        }

        /// <summary>保留原版四档精度原始比例，包括有效的零值。</summary>
        private static void AddAccuracy(
            ICollection<ChipFeatureValueContribution> target,
            ChipFeatureSourceConfig source,
            VerbProperties props,
            ChipInformationSourceKind sourceKind)
        {
            ChipFeatureSourceWorkerSupport.AddSemanticPercentage(
                target, source, "BDP_Property_AccuracyTouch", props.accuracyTouch, sourceKind);
            ChipFeatureSourceWorkerSupport.AddSemanticPercentage(
                target, source, "BDP_Property_AccuracyShort", props.accuracyShort, sourceKind);
            ChipFeatureSourceWorkerSupport.AddSemanticPercentage(
                target, source, "BDP_Property_AccuracyMedium", props.accuracyMedium, sourceKind);
            ChipFeatureSourceWorkerSupport.AddSemanticPercentage(
                target, source, "BDP_Property_AccuracyLong", props.accuracyLong, sourceKind);
        }

        /// <summary>远程齐射不发布没有意义的发射间隔。</summary>
        private static void AddBurstValues(
            ICollection<ChipFeatureValueContribution> target,
            ChipFeatureSourceConfig source,
            ChipExpressionEntryConfig entry,
            VerbProperties props,
            ChipInformationSourceKind sourceKind)
        {
            int count = entry.Execution != null && entry.Execution.HitCount > 0
                ? entry.Execution.HitCount
                : Math.Max(1, props.burstShotCount);
            ChipFeatureSourceWorkerSupport.AddSemanticInteger(
                target, source, "BDP_Property_AttackBurstCount", count, sourceKind);
            if (count <= 1)
            {
                return;
            }

            bool simultaneous = entry.Execution != null
                && entry.Execution.Rhythm == ChipAttackExecutionRhythmConfig.Simultaneous;
            ChipFeatureSourceWorkerSupport.AddSemanticValue(
                target,
                source,
                "BDP_Property_RangedShotRhythm",
                ChipInformationValue.FromTranslationKey(simultaneous
                    ? "BDP_RangedShotRhythm_Simultaneous"
                    : "BDP_RangedShotRhythm_Sequential"),
                sourceKind);
            if (!simultaneous)
            {
                ChipFeatureSourceWorkerSupport.AddSemanticDuration(
                    target,
                    source,
                    "BDP_Property_AttackBurstInterval",
                    props.ticksBetweenBurstShots / 60f,
                    sourceKind);
            }
        }

        /// <summary>读取原版投射物解析值，并仅叠加明确声明的投射物倍率。</summary>
        private static void AddProjectileValues(
            ICollection<ChipFeatureValueContribution> target,
            ChipFeatureSourceConfig source,
            ChipExpressionEntryConfig entry,
            ThingDef projectileDef,
            ChipInformationSourceKind sourceKind)
        {
            ProjectileProperties projectile = projectileDef?.projectile;
            if (projectile == null)
            {
                return;
            }

            // 保留真实定义引用，不把译后名称固化进模型；组合声明也复用此读取链。
            ChipFeatureSourceWorkerSupport.AddSemanticValue(
                target, source, "BDP_Property_Projectile",
                ChipInformationValue.FromDefReference(typeof(ThingDef), projectileDef.defName), sourceKind);

            ProjectileOverrides overrides = entry.ProjectileOverrides;
            float damageMultiplier = overrides?.damageMultiplier ?? 1f;
            float speedMultiplier = overrides?.speedMultiplier ?? 1f;
            float stoppingPowerMultiplier = overrides?.stoppingPowerMultiplier ?? 1f;
            int damage = Mathf.RoundToInt(
                projectile.GetDamageAmount((Thing)null) * damageMultiplier);
            DamageDef damageDef = ResolveDamageDef(projectile, overrides);
            float armorPenetration = ResolveArmorPenetration(projectile, damageDef);
            float speed = projectile.speed * speedMultiplier;
            float stoppingPower = projectile.stoppingPower * stoppingPowerMultiplier;

            ChipFeatureSourceWorkerSupport.AddSemanticNumber(
                target, source, "BDP_Property_DamageAmount", damage, sourceKind);
            if (projectile.damageDef != null || !string.IsNullOrWhiteSpace(overrides?.damageDef))
            {
                string damageDefName = damageDef?.defName;
                ChipFeatureSourceWorkerSupport.AddSemanticValue(
                    target, source, "BDP_Property_DamageType",
                    ChipInformationValue.FromDefReference(typeof(DamageDef), damageDefName), sourceKind);
            }

            ChipFeatureSourceWorkerSupport.AddSemanticPercentage(
                target, source, "BDP_Property_ArmorPenetration",
                armorPenetration, sourceKind);
            ChipFeatureSourceWorkerSupport.AddSemanticNumber(
                target, source, "BDP_Property_ProjectileSpeed",
                speed, sourceKind);
            ChipFeatureSourceWorkerSupport.AddSemanticNumber(
                target, source, "BDP_Property_StoppingPower",
                stoppingPower, sourceKind);
            if (projectile.explosionRadius > 0f)
            {
                ChipFeatureSourceWorkerSupport.AddSemanticNumber(
                    target, source, "BDP_Property_EffectRadius",
                    projectile.explosionRadius, sourceKind);
            }
        }

        /// <summary>解析投射物覆盖后的最终伤害类型；无效覆盖沿用原版类型。</summary>
        private static DamageDef ResolveDamageDef(
            ProjectileProperties projectile,
            ProjectileOverrides overrides)
        {
            if (!string.IsNullOrWhiteSpace(overrides?.damageDef))
            {
                DamageDef overrideDef =
                    DefDatabase<DamageDef>.GetNamedSilentFail(overrides.damageDef);
                if (overrideDef != null)
                {
                    return overrideDef;
                }
            }

            return projectile?.damageDef;
        }

        /// <summary>遵循原版投射物统计投影对覆盖伤害类型的默认穿甲处理。</summary>
        private static float ResolveArmorPenetration(
            ProjectileProperties projectile,
            DamageDef damageDef)
        {
            if (projectile == null)
            {
                return 0f;
            }

            float armorPenetration = projectile.GetArmorPenetration(null, null);
            FieldInfo baseField = typeof(ProjectileProperties).GetField(
                "armorPenetrationBase",
                BindingFlags.Instance | BindingFlags.NonPublic);
            object baseValue = baseField?.GetValue(projectile);
            bool usesDamageDefDefault = !(baseValue is float)
                || (float)baseValue < 0f;
            return usesDamageDefDefault && damageDef != null
                ? damageDef.defaultArmorPenetration
                : armorPenetration;
        }

        /// <summary>只把已启用且语义明确的 BDP 飞行模块映射为中性特征键。</summary>
        private static void AddFlightFeatures(
            ICollection<ChipFeatureValueContribution> target,
            ChipFeatureSourceConfig source,
            ChipExpressionEntryConfig entry,
            ChipInformationSourceKind sourceKind)
        {
            if (entry?.RangedModules == null)
            {
                return;
            }

            List<string> features = new List<string>();
            for (int index = 0; index < entry.RangedModules.Count; index++)
            {
                BDP.Core.AttackExecution.RangedModuleMountConfig mount =
                    entry.RangedModules[index];
                string moduleDefName = mount?.moduleDef?.defName;
                if (mount == null || !mount.enabled || string.IsNullOrWhiteSpace(moduleDefName))
                {
                    continue;
                }

                switch (moduleDefName)
                {
                    case "BDP_HomingModule_Strong":
                    case "BDP_HomingModule_Weak":
                        AddFlightFeature(features, "BDP_ProjectileFlightFeature_Homing");
                        break;
                    case "BDP_RoutePathModule":
                        AddFlightFeature(features, "BDP_ProjectileFlightFeature_Guided");
                        break;
                    case "BDP_RangedModule_DistanceRevector":
                        AddFlightFeature(features, "BDP_ProjectileFlightFeature_Turning");
                        break;
                    case "BDP_HighAngleShootingModule":
                        AddFlightFeature(features, "BDP_ProjectileFlightFeature_Arcing");
                        break;
                }
            }

            ChipFeatureSourceWorkerSupport.AddSemanticList(
                target,
                source,
                "BDP_Property_ProjectileFlightFeatures",
                features,
                sourceKind);
        }

        /// <summary>避免同一语义键因多个实现模块而重复出现。</summary>
        private static void AddFlightFeature(ICollection<string> features, string feature)
        {
            if (features != null && !features.Contains(feature))
            {
                features.Add(feature);
            }
        }
    }
}
