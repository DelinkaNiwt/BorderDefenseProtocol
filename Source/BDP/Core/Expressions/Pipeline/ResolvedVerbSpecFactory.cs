using System.Collections.Generic;
using BDP.Core.AttackExecution;
using BDP.Core.Chips;
using BDP.Core.Combos;
using RimWorld;
using Verse;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// ResolvedVerbSpec 的唯一构造与宿主表面生成入口。
    /// 所有运行时 Verb 规格都从这里收口，避免各处再临时拼装可变 VerbProps。
    /// </summary>
    internal static class ResolvedVerbSpecFactory
    {
        /// <summary>
        /// 从一份已声明的 VerbProps 构建正式规格。
        /// </summary>
        internal static ResolvedVerbSpec FromDeclared(
            VerbProperties verbProps,
            Tool tool,
            IReadOnlyList<Tool> declaredTools,
            IReadOnlyList<MeleeToolSurface> declaredMeleeToolSurfaces,
            ManeuverDef maneuver,
            DirectTargetLineOfSightRequirementConfig directTargetLineOfSightRequirement = DirectTargetLineOfSightRequirementConfig.FromVerb,
            ProjectileOverrides projectileOverrides = null)
        {
            if (verbProps == null)
            {
                return null;
            }

            return new ResolvedVerbSpec
            {
                SurfaceTemplate = verbProps,
                VerbClass = verbProps.verbClass,
                Range = verbProps.range,
                MinRange = verbProps.minRange,
                WarmupTime = verbProps.warmupTime,
                BurstShotCount = verbProps.burstShotCount > 0 ? verbProps.burstShotCount : 1,
                TicksBetweenBurstShots = verbProps.ticksBetweenBurstShots,
                ForcedMissRadius = verbProps.ForcedMissRadius,
                AccuracyTouch = verbProps.accuracyTouch,
                AccuracyShort = verbProps.accuracyShort,
                AccuracyMedium = verbProps.accuracyMedium,
                AccuracyLong = verbProps.accuracyLong,
                DefaultCooldownTime = verbProps.defaultCooldownTime,
                RequireLineOfSight = verbProps.requireLineOfSight,
                RequiresDirectTargetLineOfSight = ResolveDirectTargetLineOfSightRequirement(
                    verbProps,
                    directTargetLineOfSightRequirement),
                StopBurstWithoutLos = verbProps.stopBurstWithoutLos,
                ProjectileDef = verbProps.defaultProjectile,
                ProjectileOverrides = projectileOverrides,
                Tool = tool,
                DeclaredTools = declaredTools,
                DeclaredMeleeToolSurfaces = declaredMeleeToolSurfaces,
                Maneuver = maneuver
            };
        }

        /// <summary>
        /// 在一份基准规格上应用组合技求值后的覆盖字段。
        /// </summary>
        internal static ResolvedVerbSpec ApplyComboOverrides(
            ResolvedVerbSpec baseSpec,
            ComboResolvedVerbProps resolvedVerbProps)
        {
            if (baseSpec == null)
            {
                return null;
            }

            ResolvedVerbSpec spec = new ResolvedVerbSpec
            {
                SurfaceTemplate = baseSpec.SurfaceTemplate,
                VerbClass = baseSpec.VerbClass,
                Range = baseSpec.Range,
                MinRange = baseSpec.MinRange,
                WarmupTime = baseSpec.WarmupTime,
                BurstShotCount = baseSpec.BurstShotCount,
                TicksBetweenBurstShots = baseSpec.TicksBetweenBurstShots,
                ForcedMissRadius = baseSpec.ForcedMissRadius,
                AccuracyTouch = baseSpec.AccuracyTouch,
                AccuracyShort = baseSpec.AccuracyShort,
                AccuracyMedium = baseSpec.AccuracyMedium,
                AccuracyLong = baseSpec.AccuracyLong,
                DefaultCooldownTime = baseSpec.DefaultCooldownTime,
                RequireLineOfSight = baseSpec.RequireLineOfSight,
                RequiresDirectTargetLineOfSight = baseSpec.RequiresDirectTargetLineOfSight,
                StopBurstWithoutLos = baseSpec.StopBurstWithoutLos,
                ProjectileDef = baseSpec.ProjectileDef,
                ProjectileOverrides = baseSpec.ProjectileOverrides,
                Tool = baseSpec.Tool,
                DeclaredTools = baseSpec.DeclaredTools,
                DeclaredMeleeToolSurfaces = baseSpec.DeclaredMeleeToolSurfaces,
                Maneuver = baseSpec.Maneuver
            };

            if (resolvedVerbProps != null)
            {
                if (resolvedVerbProps.Range != null && resolvedVerbProps.Range.HasResolvedValue)
                {
                    spec.Range = resolvedVerbProps.Range.ResolvedValue;
                }

                if (resolvedVerbProps.MinRange != null && resolvedVerbProps.MinRange.HasResolvedValue)
                {
                    spec.MinRange = resolvedVerbProps.MinRange.ResolvedValue;
                }

                if (resolvedVerbProps.WarmupTime != null && resolvedVerbProps.WarmupTime.HasResolvedValue)
                {
                    spec.WarmupTime = resolvedVerbProps.WarmupTime.ResolvedValue;
                }

                if (resolvedVerbProps.BurstShotCount != null && resolvedVerbProps.BurstShotCount.HasResolvedValue)
                {
                    spec.BurstShotCount = resolvedVerbProps.BurstShotCount.ResolvedValue;
                }

                if (resolvedVerbProps.TicksBetweenBurstShots != null && resolvedVerbProps.TicksBetweenBurstShots.HasResolvedValue)
                {
                    spec.TicksBetweenBurstShots = resolvedVerbProps.TicksBetweenBurstShots.ResolvedValue;
                }

                if (resolvedVerbProps.ForcedMissRadius != null && resolvedVerbProps.ForcedMissRadius.HasResolvedValue)
                {
                    spec.ForcedMissRadius = resolvedVerbProps.ForcedMissRadius.ResolvedValue;
                }

                if (resolvedVerbProps.AccuracyTouch != null && resolvedVerbProps.AccuracyTouch.HasResolvedValue)
                {
                    spec.AccuracyTouch = resolvedVerbProps.AccuracyTouch.ResolvedValue;
                }

                if (resolvedVerbProps.AccuracyShort != null && resolvedVerbProps.AccuracyShort.HasResolvedValue)
                {
                    spec.AccuracyShort = resolvedVerbProps.AccuracyShort.ResolvedValue;
                }

                if (resolvedVerbProps.AccuracyMedium != null && resolvedVerbProps.AccuracyMedium.HasResolvedValue)
                {
                    spec.AccuracyMedium = resolvedVerbProps.AccuracyMedium.ResolvedValue;
                }

                if (resolvedVerbProps.AccuracyLong != null && resolvedVerbProps.AccuracyLong.HasResolvedValue)
                {
                    spec.AccuracyLong = resolvedVerbProps.AccuracyLong.ResolvedValue;
                }

                if (resolvedVerbProps.DefaultCooldownTime != null && resolvedVerbProps.DefaultCooldownTime.HasResolvedValue)
                {
                    spec.DefaultCooldownTime = resolvedVerbProps.DefaultCooldownTime.ResolvedValue;
                }

                if (resolvedVerbProps.DefaultProjectile != null)
                {
                    spec.ProjectileDef = resolvedVerbProps.DefaultProjectile;
                }
            }

            if (spec.BurstShotCount <= 0)
            {
                spec.BurstShotCount = 1;
            }

            return spec;
        }

        /// <summary>
        /// 从来源规格和组合技字段求值结果中解析组合技最终 Verb 规格。
        /// 显式条目优先，缺失字段才用组合技求值规则补齐。
        /// </summary>
        internal static ResolvedVerbSpec ResolveComboSpec(
            VerbProperties fallbackVerbProps,
            ResolvedVerbSpec fallbackVerbSpec,
            ComboResolvedVerbProps resolvedVerbProps)
        {
            ResolvedVerbSpec baseSpec = fallbackVerbSpec
                ?? FromDeclared(
                    fallbackVerbProps,
                    null,
                    new List<Tool>(),
                    new List<MeleeToolSurface>(),
                    null);
            return ApplyComboOverrides(baseSpec, resolvedVerbProps);
        }

        /// <summary>
        /// 把作者声明的必要直射策略正规化成运行时布尔真值。
        /// 这里刻意和 RequireLineOfSight 分开，避免把模块内部 LOS 需求误当成 dual 入口准入条件。
        /// </summary>
        private static bool ResolveDirectTargetLineOfSightRequirement(
            VerbProperties verbProps,
            DirectTargetLineOfSightRequirementConfig directTargetLineOfSightRequirement)
        {
            switch (directTargetLineOfSightRequirement)
            {
                case DirectTargetLineOfSightRequirementConfig.Required:
                    return true;
                case DirectTargetLineOfSightRequirementConfig.NotRequired:
                    return false;
                case DirectTargetLineOfSightRequirementConfig.FromVerb:
                default:
                    return verbProps != null && verbProps.requireLineOfSight;
            }
        }

        /// <summary>
        /// 从正式规格生成给 Verse 壳层消费的 VerbProps 表面。
        /// 这里只复制公开支持字段，并把运行时覆盖字段显式落在副本上。
        /// </summary>
        internal static VerbProperties CreateSurfaceVerbProps(ResolvedVerbSpec spec)
        {
            if (spec?.SurfaceTemplate == null)
            {
                return null;
            }

            VerbProperties source = spec.SurfaceTemplate;
            VerbProperties target = new VerbProperties
            {
                category = source.category,
                verbClass = spec.VerbClass ?? source.verbClass,
                label = source.label,
                untranslatedLabel = source.untranslatedLabel,
                isPrimary = source.isPrimary,
                violent = source.violent,
                minRange = spec.MinRange,
                range = spec.Range,
                rangeStat = source.rangeStat,
                burstShotCount = spec.BurstShotCount > 0 ? spec.BurstShotCount : 1,
                ticksBetweenBurstShots = spec.TicksBetweenBurstShots,
                showBurstShotStats = source.showBurstShotStats,
                noiseRadius = source.noiseRadius,
                hasStandardCommand = source.hasStandardCommand,
                targetable = source.targetable,
                nonInterruptingSelfCast = source.nonInterruptingSelfCast,
                targetParams = source.targetParams,
                requireLineOfSight = source.requireLineOfSight,
                mustCastOnOpenGround = source.mustCastOnOpenGround,
                forceNormalTimeSpeed = source.forceNormalTimeSpeed,
                onlyManualCast = source.onlyManualCast,
                stopBurstWithoutLos = source.stopBurstWithoutLos,
                surpriseAttack = source.surpriseAttack,
                commonality = source.commonality,
                minIntelligence = source.minIntelligence,
                consumeFuelPerShot = source.consumeFuelPerShot,
                consumeFuelPerBurst = source.consumeFuelPerBurst,
                stunTargetOnCastStart = source.stunTargetOnCastStart,
                invalidTargetPawn = source.invalidTargetPawn,
                commonalityVsEdificeFactor = source.commonalityVsEdificeFactor,
                flammabilityAttachFireChanceCurve = source.flammabilityAttachFireChanceCurve,
                useableInPocketMaps = source.useableInPocketMaps,
                useableInVacuum = source.useableInVacuum,
                mouseTargetingText = source.mouseTargetingText,
                layerWhitelist = source.layerWhitelist != null ? new List<PlanetLayerDef>(source.layerWhitelist) : null,
                layerBlacklist = source.layerBlacklist != null ? new List<PlanetLayerDef>(source.layerBlacklist) : null,
                warmupTime = spec.WarmupTime,
                defaultCooldownTime = spec.DefaultCooldownTime > 0f ? spec.DefaultCooldownTime : source.defaultCooldownTime,
                commandIcon = source.commandIcon,
                soundCast = source.soundCast,
                soundCastTail = source.soundCastTail,
                soundAiming = source.soundAiming,
                muzzleFlashScale = source.muzzleFlashScale,
                impactMote = source.impactMote,
                impactFleck = source.impactFleck,
                drawAimPie = source.drawAimPie,
                warmupEffecter = source.warmupEffecter,
                drawHighlightWithLineOfSight = source.drawHighlightWithLineOfSight,
                aimingLineMote = source.aimingLineMote,
                aimingLineMoteFixedLength = source.aimingLineMoteFixedLength,
                aimingChargeMote = source.aimingChargeMote,
                aimingChargeMoteOffset = source.aimingChargeMoteOffset,
                aimingTargetMote = source.aimingTargetMote,
                aimingTargetEffecter = source.aimingTargetEffecter,
                explosionRadiusRingColor = source.explosionRadiusRingColor,
                linkedBodyPartsGroup = source.linkedBodyPartsGroup,
                ensureLinkedBodyPartsGroupAlwaysUsable = source.ensureLinkedBodyPartsGroupAlwaysUsable,
                meleeDamageDef = source.meleeDamageDef,
                meleeDamageBaseAmount = source.meleeDamageBaseAmount,
                meleeArmorPenetrationBase = source.meleeArmorPenetrationBase,
                ai_IsWeapon = source.ai_IsWeapon,
                ai_IsBuildingDestroyer = source.ai_IsBuildingDestroyer,
                ai_AvoidFriendlyFireRadius = source.ai_AvoidFriendlyFireRadius,
                ai_RangedAlawaysShootGroundBelowTarget = source.ai_RangedAlawaysShootGroundBelowTarget,
                ai_IsDoorDestroyer = source.ai_IsDoorDestroyer,
                ai_ProjectileLaunchingIgnoresMeleeThreats = source.ai_ProjectileLaunchingIgnoresMeleeThreats,
                ai_TargetHasRangedAttackScoreOffset = source.ai_TargetHasRangedAttackScoreOffset,
                defaultProjectile = spec.ProjectileDef ?? source.defaultProjectile,
                forcedMissEvenDispersal = source.forcedMissEvenDispersal,
                accuracyTouch = spec.AccuracyTouch > 0f ? spec.AccuracyTouch : source.accuracyTouch,
                accuracyShort = spec.AccuracyShort > 0f ? spec.AccuracyShort : source.accuracyShort,
                accuracyMedium = spec.AccuracyMedium > 0f ? spec.AccuracyMedium : source.accuracyMedium,
                accuracyLong = spec.AccuracyLong > 0f ? spec.AccuracyLong : source.accuracyLong,
                canGoWild = source.canGoWild,
                highlightColor = source.highlightColor,
                secondaryHighlightColor = source.secondaryHighlightColor,
                bodypartTagTarget = source.bodypartTagTarget,
                rangedFireRulepack = source.rangedFireRulepack,
                soundLanding = source.soundLanding,
                flightEffecterDef = source.flightEffecterDef,
                flyWithCarriedThing = source.flyWithCarriedThing,
                workModeDef = source.workModeDef
            };
            return target;
        }
    }
}
