using BDP.Core.CombatModel;
using BDP.Core.Expressions;
using BDP.Core.Requirements;
using System.Collections.Generic;
using Verse;

namespace BDP.Core.Combos
{
    /// <summary>
    /// 组合技定义契约解释器。
    /// 它把作者写在 ComboDef 上的声明翻译成主模组承认的正式结构。
    /// </summary>
    internal sealed class ComboDefinitionContractResolver
    {
        /// <summary>
        /// 解释指定 ComboDef 的正式契约。
        /// </summary>
        public ComboDefinitionContract Resolve(ComboDef comboDef)
        {
            ComboDefinitionConfig config = comboDef != null ? comboDef.ToConfig() : null;
            string firstSourceActionDefName = config != null ? config.firstSourceActionDefName : null;
            string secondSourceActionDefName = config != null ? config.secondSourceActionDefName : null;
            return new ComboDefinitionContract
            {
                Definition = comboDef,
                Config = config,
                FirstSourceActionDefName = firstSourceActionDefName,
                SecondSourceActionDefName = secondSourceActionDefName,
                FirstSourceAdmission = ResolveSourceAdmission(config?.FirstSourceAdmission),
                SecondSourceAdmission = ResolveSourceAdmission(config?.SecondSourceAdmission),
                RequireSameSourceVariant = config != null && config.RequireSameSourceVariant,
                UseRequirements = config?.UseRequirements != null
                    ? new List<PawnRequirement>(config.UseRequirements).AsReadOnly()
                    : new List<PawnRequirement>().AsReadOnly(),
                Expression = ResolveExpression(config != null ? config.Expression : null)
            };
        }

        /// <summary>把可空来源准入配置复制为稳定只读契约。</summary>
        private static ComboSourceAdmissionContract ResolveSourceAdmission(
            ComboSourceAdmissionConfig config)
        {
            if (config == null)
            {
                return null;
            }

            return new ComboSourceAdmissionContract
            {
                AllowedProfessions = Copy(config.AllowedProfessions),
                DeniedProfessions = Copy(config.DeniedProfessions),
                AllowedCategories = Copy(config.AllowedCategories),
                DeniedCategories = Copy(config.DeniedCategories),
                AllowedTags = Copy(config.AllowedTags),
                RequiredTags = Copy(config.RequiredTags),
                DeniedTags = Copy(config.DeniedTags),
                AllowedSourceVariants = Copy(config.AllowedSourceVariants),
                DeniedSourceVariants = Copy(config.DeniedSourceVariants)
            };
        }

        /// <summary>复制字符串列表并固定只读边界。</summary>
        private static IReadOnlyList<string> Copy(List<string> values)
        {
            return values != null
                ? new List<string>(values).AsReadOnly()
                : new List<string>().AsReadOnly();
        }

        /// <summary>
        /// 解释组合技表达声明句柄。
        /// </summary>
        private static ComboExpressionContractHandle ResolveExpression(ComboExpressionConfig config)
        {
            return new ComboExpressionContractHandle
            {
                HasExpressionBlock = config != null,
                Config = config,
                StructureKey = config != null ? "Entries" : null
            };
        }

        /// <summary>
        /// 解释组合技 VerbProps 的字段级求值结果。
        /// 这里故意只消费两侧正式结果。
        /// 它只负责 Verb 字段补值，不承担“来源侧上下文”定义。
        /// </summary>
        public ComboResolvedVerbProps ResolveVerbProps(
            ComboExpressionEntryConfig entryConfig,
            FormalExpressionResult firstSourceResult,
            FormalExpressionResult secondSourceResult)
        {
            VerbProperties firstSourceVerbProps = firstSourceResult != null ? firstSourceResult.VerbProps : null;
            VerbProperties secondSourceVerbProps = secondSourceResult != null ? secondSourceResult.VerbProps : null;
            ComboVerbPropsResolutionConfig resolution = entryConfig != null ? entryConfig.VerbPropsResolve : null;
            // 条目 VerbProps 增量覆盖层：作者显式声明的字段级 delta。
            // 非 null 字段作为 explicitValue 优先于 VerbPropsResolve 模式。
            VerbPropsOverlay overlay = entryConfig?.VerbProps;

            return new ComboResolvedVerbProps
            {
                Range = ResolveFloatField(
                    overlay?.range,
                    resolution != null ? resolution.RangeResolve : null,
                    firstSourceVerbProps != null ? firstSourceVerbProps.range : 0f,
                    secondSourceVerbProps != null ? secondSourceVerbProps.range : 0f),
                WarmupTime = ResolveFloatField(
                    overlay?.warmupTime,
                    resolution != null ? resolution.WarmupTimeResolve : null,
                    firstSourceVerbProps != null ? firstSourceVerbProps.warmupTime : 0f,
                    secondSourceVerbProps != null ? secondSourceVerbProps.warmupTime : 0f),
                BurstShotCount = ResolveIntField(
                    overlay?.burstShotCount,
                    resolution != null ? resolution.BurstShotCountResolve : null,
                    firstSourceVerbProps != null ? firstSourceVerbProps.burstShotCount : 1,
                    secondSourceVerbProps != null ? secondSourceVerbProps.burstShotCount : 1),
                TicksBetweenBurstShots = ResolveIntField(
                    overlay?.ticksBetweenBurstShots,
                    resolution != null ? resolution.TicksBetweenBurstShotsResolve : null,
                    firstSourceVerbProps != null ? firstSourceVerbProps.ticksBetweenBurstShots : 0,
                    secondSourceVerbProps != null ? secondSourceVerbProps.ticksBetweenBurstShots : 0),
                MinRange = ResolveFloatField(
                    overlay?.minRange,
                    resolution != null ? resolution.MinRangeResolve : null,
                    firstSourceVerbProps != null ? firstSourceVerbProps.minRange : 0f,
                    secondSourceVerbProps != null ? secondSourceVerbProps.minRange : 0f),
                ForcedMissRadius = ResolveFloatField(
                    overlay?.forcedMissRadius,
                    resolution != null ? resolution.ForcedMissRadiusResolve : null,
                    firstSourceVerbProps != null ? firstSourceVerbProps.ForcedMissRadius : 0f,
                    secondSourceVerbProps != null ? secondSourceVerbProps.ForcedMissRadius : 0f),
                AccuracyTouch = ResolveFloatField(
                    overlay?.accuracyTouch,
                    resolution != null ? resolution.AccuracyTouchResolve : null,
                    firstSourceVerbProps != null ? firstSourceVerbProps.accuracyTouch : 0f,
                    secondSourceVerbProps != null ? secondSourceVerbProps.accuracyTouch : 0f),
                AccuracyShort = ResolveFloatField(
                    overlay?.accuracyShort,
                    resolution != null ? resolution.AccuracyShortResolve : null,
                    firstSourceVerbProps != null ? firstSourceVerbProps.accuracyShort : 0f,
                    secondSourceVerbProps != null ? secondSourceVerbProps.accuracyShort : 0f),
                AccuracyMedium = ResolveFloatField(
                    overlay?.accuracyMedium,
                    resolution != null ? resolution.AccuracyMediumResolve : null,
                    firstSourceVerbProps != null ? firstSourceVerbProps.accuracyMedium : 0f,
                    secondSourceVerbProps != null ? secondSourceVerbProps.accuracyMedium : 0f),
                AccuracyLong = ResolveFloatField(
                    overlay?.accuracyLong,
                    resolution != null ? resolution.AccuracyLongResolve : null,
                    firstSourceVerbProps != null ? firstSourceVerbProps.accuracyLong : 0f,
                    secondSourceVerbProps != null ? secondSourceVerbProps.accuracyLong : 0f),
                DefaultCooldownTime = ResolveFloatField(
                    overlay?.defaultCooldownTime,
                    resolution != null ? resolution.DefaultCooldownTimeResolve : null,
                    firstSourceVerbProps != null ? firstSourceVerbProps.defaultCooldownTime : 0f,
                    secondSourceVerbProps != null ? secondSourceVerbProps.defaultCooldownTime : 0f),
                DefaultProjectile = overlay?.defaultProjectile
            };
        }

        /// <summary>
        /// 解释组合技执行节奏字段的求值结果。
        /// 这里故意只消费两侧正式结果。
        /// 当前先只覆盖近战最小节奏字段，不把它误当成来源侧上下文入口。
        /// </summary>
        public ComboResolvedExecution ResolveExecution(
            ComboExpressionEntryConfig entryConfig,
            FormalExpressionResult firstSourceResult,
            FormalExpressionResult secondSourceResult)
        {
            SingleAttackExecutionStyle firstSourceStyle = firstSourceResult != null ? firstSourceResult.ExecutionStyle?.Single : null;
            SingleAttackExecutionStyle secondSourceStyle = secondSourceResult != null ? secondSourceResult.ExecutionStyle?.Single : null;
            ComboExecutionResolutionConfig resolution = entryConfig != null ? entryConfig.ExecutionResolve : null;
            ChipAttackExecutionConfig explicitExecution = entryConfig != null ? entryConfig.Execution : null;
            int? explicitHitCount = explicitExecution != null && explicitExecution.HitCount > 0
                ? explicitExecution.HitCount
                : (int?)null;
            int? explicitHitIntervalTicks = explicitExecution != null && explicitExecution.HitIntervalTicks > 0
                ? explicitExecution.HitIntervalTicks
                : (int?)null;
            RangedExecutionRhythm? explicitRhythm = ResolveExplicitRangedRhythm(
                explicitExecution != null ? explicitExecution.Rhythm : ChipAttackExecutionRhythmConfig.None);

            return new ComboResolvedExecution
            {
                HitCount = ResolveIntField(
                    explicitHitCount,
                    resolution != null ? resolution.HitCountResolve : null,
                    firstSourceStyle != null && firstSourceStyle.meleeHitCount > 0 ? firstSourceStyle.meleeHitCount : 1,
                    secondSourceStyle != null && secondSourceStyle.meleeHitCount > 0 ? secondSourceStyle.meleeHitCount : 1),
                HitIntervalTicks = ResolveIntField(
                    explicitHitIntervalTicks,
                    resolution != null ? resolution.HitIntervalTicksResolve : null,
                    firstSourceStyle != null ? firstSourceStyle.meleeHitIntervalTicks : 0,
                    secondSourceStyle != null ? secondSourceStyle.meleeHitIntervalTicks : 0),
                Rhythm = ResolveRhythmField(
                    explicitRhythm,
                    resolution != null ? resolution.RhythmResolve : null,
                    firstSourceStyle,
                    secondSourceStyle)
            };
        }

        /// <summary>
        /// 按统一协议解析 float 字段。
        /// </summary>
        private static ComboResolvedFieldValue<float> ResolveFloatField(
            float? explicitValue,
            ComboValueResolveMode? declaredMode,
            float firstSourceValue,
            float secondSourceValue)
        {
            return ComboSourceFieldResolver.ResolveFloat(
                explicitValue,
                declaredMode,
                firstSourceValue,
                secondSourceValue);
        }

        /// <summary>
        /// 解析远程射击节奏字段。
        /// 只支持 FollowFirstSource / FollowSecondSource 单侧跟随，不做数值合成。
        /// </summary>
        private static ComboResolvedFieldValue<RangedExecutionRhythm> ResolveRhythmField(
            RangedExecutionRhythm? explicitValue,
            ComboValueResolveMode? declaredMode,
            SingleAttackExecutionStyle firstSourceStyle,
            SingleAttackExecutionStyle secondSourceStyle)
        {
            RangedExecutionRhythm firstSourceRhythm = firstSourceStyle != null
                ? firstSourceStyle.RangedRhythm
                : RangedExecutionRhythm.None;
            RangedExecutionRhythm secondSourceRhythm = secondSourceStyle != null
                ? secondSourceStyle.RangedRhythm
                : RangedExecutionRhythm.None;

            ComboResolvedFieldValue<RangedExecutionRhythm> result = new ComboResolvedFieldValue<RangedExecutionRhythm>
            {
                HasExplicitValue = explicitValue.HasValue,
                ExplicitValue = explicitValue.HasValue ? explicitValue.Value : RangedExecutionRhythm.None,
                ResolveMode = declaredMode
            };

            if (explicitValue.HasValue)
            {
                result.HasResolvedValue = true;
                result.ResolvedValue = explicitValue.Value;
                return result;
            }

            if (!declaredMode.HasValue)
            {
                return result;
            }

            result.HasResolvedValue = true;
            switch (declaredMode.Value)
            {
                case ComboValueResolveMode.FollowFirstSource:
                    result.ResolvedValue = firstSourceRhythm;
                    break;
                case ComboValueResolveMode.FollowSecondSource:
                    result.ResolvedValue = secondSourceRhythm;
                    break;
                default:
                    result.HasResolvedValue = false;
                    break;
            }

            return result;
        }

        /// <summary>把作者侧远程节奏声明翻译成正式节奏；未声明时返回空值。</summary>
        private static RangedExecutionRhythm? ResolveExplicitRangedRhythm(
            ChipAttackExecutionRhythmConfig rhythm)
        {
            switch (rhythm)
            {
                case ChipAttackExecutionRhythmConfig.Simultaneous:
                    return RangedExecutionRhythm.Simultaneous;
                case ChipAttackExecutionRhythmConfig.Sequential:
                case ChipAttackExecutionRhythmConfig.Normal:
                    return RangedExecutionRhythm.Sequential;
                default:
                    return null;
            }
        }

        /// <summary>
        /// 按统一协议解析 int 字段。
        /// </summary>
        private static ComboResolvedFieldValue<int> ResolveIntField(
            int? explicitValue,
            ComboValueResolveMode? declaredMode,
            int firstSourceValue,
            int secondSourceValue)
        {
            return ComboSourceFieldResolver.ResolveInt(
                explicitValue,
                declaredMode,
                firstSourceValue,
                secondSourceValue);
        }
    }
}
