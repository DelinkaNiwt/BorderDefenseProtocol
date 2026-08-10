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
            string chipADefName = config != null ? config.chipA : null;
            string chipBDefName = config != null ? config.chipB : null;
            return new ComboDefinitionContract
            {
                Definition = comboDef,
                Config = config,
                ChipADefName = chipADefName,
                ChipBDefName = chipBDefName,
                UseRequirements = config?.UseRequirements != null
                    ? new List<PawnRequirement>(config.UseRequirements).AsReadOnly()
                    : new List<PawnRequirement>().AsReadOnly(),
                Expression = ResolveExpression(config != null ? config.Expression : null)
            };
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
            FormalExpressionResult chipASourceResult,
            FormalExpressionResult chipBSourceResult)
        {
            VerbProperties chipAVerbProps = chipASourceResult != null ? chipASourceResult.VerbProps : null;
            VerbProperties chipBVerbProps = chipBSourceResult != null ? chipBSourceResult.VerbProps : null;
            ComboVerbPropsResolutionConfig resolution = entryConfig != null ? entryConfig.VerbPropsResolve : null;
            // 条目 VerbProps 增量覆盖层：作者显式声明的字段级 delta。
            // 非 null 字段作为 explicitValue 优先于 VerbPropsResolve 模式。
            VerbPropsOverlay overlay = entryConfig?.VerbProps;

            return new ComboResolvedVerbProps
            {
                Range = ResolveFloatField(
                    overlay?.range,
                    resolution != null ? resolution.RangeResolve : null,
                    chipAVerbProps != null ? chipAVerbProps.range : 0f,
                    chipBVerbProps != null ? chipBVerbProps.range : 0f),
                WarmupTime = ResolveFloatField(
                    overlay?.warmupTime,
                    resolution != null ? resolution.WarmupTimeResolve : null,
                    chipAVerbProps != null ? chipAVerbProps.warmupTime : 0f,
                    chipBVerbProps != null ? chipBVerbProps.warmupTime : 0f),
                BurstShotCount = ResolveIntField(
                    overlay?.burstShotCount,
                    resolution != null ? resolution.BurstShotCountResolve : null,
                    chipAVerbProps != null ? chipAVerbProps.burstShotCount : 1,
                    chipBVerbProps != null ? chipBVerbProps.burstShotCount : 1),
                TicksBetweenBurstShots = ResolveIntField(
                    overlay?.ticksBetweenBurstShots,
                    resolution != null ? resolution.TicksBetweenBurstShotsResolve : null,
                    chipAVerbProps != null ? chipAVerbProps.ticksBetweenBurstShots : 0,
                    chipBVerbProps != null ? chipBVerbProps.ticksBetweenBurstShots : 0),
                MinRange = ResolveFloatField(
                    overlay?.minRange,
                    resolution != null ? resolution.MinRangeResolve : null,
                    chipAVerbProps != null ? chipAVerbProps.minRange : 0f,
                    chipBVerbProps != null ? chipBVerbProps.minRange : 0f),
                ForcedMissRadius = ResolveFloatField(
                    overlay?.forcedMissRadius,
                    resolution != null ? resolution.ForcedMissRadiusResolve : null,
                    chipAVerbProps != null ? chipAVerbProps.ForcedMissRadius : 0f,
                    chipBVerbProps != null ? chipBVerbProps.ForcedMissRadius : 0f),
                AccuracyTouch = ResolveFloatField(
                    overlay?.accuracyTouch,
                    resolution != null ? resolution.AccuracyTouchResolve : null,
                    chipAVerbProps != null ? chipAVerbProps.accuracyTouch : 0f,
                    chipBVerbProps != null ? chipBVerbProps.accuracyTouch : 0f),
                AccuracyShort = ResolveFloatField(
                    overlay?.accuracyShort,
                    resolution != null ? resolution.AccuracyShortResolve : null,
                    chipAVerbProps != null ? chipAVerbProps.accuracyShort : 0f,
                    chipBVerbProps != null ? chipBVerbProps.accuracyShort : 0f),
                AccuracyMedium = ResolveFloatField(
                    overlay?.accuracyMedium,
                    resolution != null ? resolution.AccuracyMediumResolve : null,
                    chipAVerbProps != null ? chipAVerbProps.accuracyMedium : 0f,
                    chipBVerbProps != null ? chipBVerbProps.accuracyMedium : 0f),
                AccuracyLong = ResolveFloatField(
                    overlay?.accuracyLong,
                    resolution != null ? resolution.AccuracyLongResolve : null,
                    chipAVerbProps != null ? chipAVerbProps.accuracyLong : 0f,
                    chipBVerbProps != null ? chipBVerbProps.accuracyLong : 0f),
                DefaultCooldownTime = ResolveFloatField(
                    overlay?.defaultCooldownTime,
                    resolution != null ? resolution.DefaultCooldownTimeResolve : null,
                    chipAVerbProps != null ? chipAVerbProps.defaultCooldownTime : 0f,
                    chipBVerbProps != null ? chipBVerbProps.defaultCooldownTime : 0f),
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
            FormalExpressionResult chipASourceResult,
            FormalExpressionResult chipBSourceResult)
        {
            SingleAttackExecutionStyle chipAStyle = chipASourceResult != null ? chipASourceResult.ExecutionStyle?.Single : null;
            SingleAttackExecutionStyle chipBStyle = chipBSourceResult != null ? chipBSourceResult.ExecutionStyle?.Single : null;
            ComboExecutionResolutionConfig resolution = entryConfig != null ? entryConfig.ExecutionResolve : null;

            return new ComboResolvedExecution
            {
                HitCount = ResolveIntField(
                    null,
                    resolution != null ? resolution.HitCountResolve : null,
                    chipAStyle != null && chipAStyle.meleeHitCount > 0 ? chipAStyle.meleeHitCount : 1,
                    chipBStyle != null && chipBStyle.meleeHitCount > 0 ? chipBStyle.meleeHitCount : 1),
                HitIntervalTicks = ResolveIntField(
                    null,
                    resolution != null ? resolution.HitIntervalTicksResolve : null,
                    chipAStyle != null ? chipAStyle.meleeHitIntervalTicks : 0,
                    chipBStyle != null ? chipBStyle.meleeHitIntervalTicks : 0),
                Rhythm = ResolveRhythmField(
                    resolution != null ? resolution.RhythmResolve : null,
                    chipAStyle,
                    chipBStyle)
            };
        }

        /// <summary>
        /// 按统一协议解析 float 字段。
        /// </summary>
        private static ComboResolvedFieldValue<float> ResolveFloatField(
            float? explicitValue,
            ComboValueResolveMode? declaredMode,
            float chipAValue,
            float chipBValue)
        {
            return ComboSourceFieldResolver.ResolveFloat(
                explicitValue,
                declaredMode,
                chipAValue,
                chipBValue);
        }

        /// <summary>
        /// 解析远程射击节奏字段。
        /// 只支持 FollowMain / FollowSub 单侧跟随，不做数值合成。
        /// </summary>
        private static ComboResolvedFieldValue<RangedExecutionRhythm> ResolveRhythmField(
            ComboValueResolveMode? declaredMode,
            SingleAttackExecutionStyle chipAStyle,
            SingleAttackExecutionStyle chipBStyle)
        {
            RangedExecutionRhythm chipARhythm = chipAStyle != null
                ? chipAStyle.RangedRhythm
                : RangedExecutionRhythm.None;
            RangedExecutionRhythm chipBRhythm = chipBStyle != null
                ? chipBStyle.RangedRhythm
                : RangedExecutionRhythm.None;

            ComboResolvedFieldValue<RangedExecutionRhythm> result = new ComboResolvedFieldValue<RangedExecutionRhythm>
            {
                HasExplicitValue = false,
                ResolveMode = declaredMode
            };

            if (!declaredMode.HasValue)
            {
                return result;
            }

            result.HasResolvedValue = true;
            switch (declaredMode.Value)
            {
                case ComboValueResolveMode.FollowChipMain:
                    result.ResolvedValue = chipARhythm;
                    break;
                case ComboValueResolveMode.FollowChipSub:
                    result.ResolvedValue = chipBRhythm;
                    break;
                default:
                    result.HasResolvedValue = false;
                    break;
            }

            return result;
        }

        /// <summary>
        /// 按统一协议解析 int 字段。
        /// </summary>
        private static ComboResolvedFieldValue<int> ResolveIntField(
            int? explicitValue,
            ComboValueResolveMode? declaredMode,
            int chipAValue,
            int chipBValue)
        {
            return ComboSourceFieldResolver.ResolveInt(
                explicitValue,
                declaredMode,
                chipAValue,
                chipBValue);
        }
    }
}
