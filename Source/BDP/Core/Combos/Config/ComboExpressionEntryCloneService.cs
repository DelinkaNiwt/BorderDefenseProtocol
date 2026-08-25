using System.Collections.Generic;
using BDP.Core.AttackExecution;
using BDP.Core.Chips;
using BDP.Core.Expressions;
using Verse;

namespace BDP.Core.Combos
{
    /// <summary>
    /// 组合表达条目副本服务。
    /// 组合级修正只能作用于副本，不能回写 DefDatabase 缓存中的原始 ComboDef 条目。
    /// </summary>
    internal static class ComboExpressionEntryCloneService
    {
        /// <summary>
        /// 深复制组合表达条目集合。
        /// </summary>
        internal static List<ComboExpressionEntryConfig> CloneEntries(
            IReadOnlyList<ComboExpressionEntryConfig> source)
        {
            List<ComboExpressionEntryConfig> result = new List<ComboExpressionEntryConfig>();
            if (source == null)
            {
                return result;
            }

            for (int index = 0; index < source.Count; index++)
            {
                ComboExpressionEntryConfig entry = Clone(source[index]);
                if (entry != null)
                {
                    result.Add(entry);
                }
            }

            return result;
        }

        /// <summary>
        /// 深复制单条组合表达条目。
        /// Def 引用保持共享，作者配置和列表容器全部复制。
        /// </summary>
        internal static ComboExpressionEntryConfig Clone(ComboExpressionEntryConfig source)
        {
            if (source == null)
            {
                return null;
            }

            return new ComboExpressionEntryConfig
            {
                Id = source.Id,
                DisplayLabel = source.DisplayLabel,
                DisplayLabelKey = source.DisplayLabelKey,
                RoleKey = source.RoleKey,
                Tags = CloneStrings(source.Tags),
                Conditions = CloneConditions(source.Conditions),
                Trion = CloneTrion(source.Trion),
                SemanticSourceKind = source.SemanticSourceKind,
                Presentation = ClonePresentation(source.Presentation),
                Kind = source.Kind,
                RelationKind = source.RelationKind,
                ParentEntryId = source.ParentEntryId,
                WeaponMode = source.WeaponMode,
                VerbProps = CloneVerbProps(source.VerbProps),
                DirectTargetLineOfSight = source.DirectTargetLineOfSight,
                VerbPropsResolve = CloneVerbPropsResolve(source.VerbPropsResolve),
                Tool = source.Tool,
                tools = source.tools != null ? new List<Tool>(source.tools) : new List<Tool>(),
                Maneuver = source.Maneuver,
                Execution = CloneExecution(source.Execution),
                ExecutionResolve = CloneExecutionResolve(source.ExecutionResolve),
                TrionResolve = CloneTrionResolve(source.TrionResolve),
                AbilityDefName = source.AbilityDefName,
                HediffDefName = source.HediffDefName,
                HediffApplyModeKey = source.HediffApplyModeKey,
                PassiveKey = source.PassiveKey,
                ExposedData = CloneExposedData(source.ExposedData),
                RangedModules = CloneRangedModules(source.RangedModules),
                ProjectileOverrides = source.ProjectileOverrides != null
                    ? source.ProjectileOverrides.Clone()
                    : null
            };
        }

        /// <summary>复制字符串列表。</summary>
        private static List<string> CloneStrings(IReadOnlyList<string> source)
        {
            return source != null ? new List<string>(source) : new List<string>();
        }

        /// <summary>复制表达成立条件列表。</summary>
        private static List<ExpressionSourceConditionConfig> CloneConditions(
            IReadOnlyList<ExpressionSourceConditionConfig> source)
        {
            List<ExpressionSourceConditionConfig> result = new List<ExpressionSourceConditionConfig>();
            if (source == null)
            {
                return result;
            }

            for (int index = 0; index < source.Count; index++)
            {
                ExpressionSourceConditionConfig condition = source[index];
                if (condition == null)
                {
                    continue;
                }

                result.Add(new ExpressionSourceConditionConfig
                {
                    ConditionKey = condition.ConditionKey,
                    Required = condition.Required,
                    Parameters = CloneStrings(condition.Parameters)
                });
            }

            return result;
        }

        /// <summary>复制 Trion 配置。</summary>
        private static ExpressionSourceTrionConfig CloneTrion(ExpressionSourceTrionConfig source)
        {
            if (source == null)
            {
                return null;
            }

            return new ExpressionSourceTrionConfig
            {
                UseCost = source.UseCost,
                MinimumRequired = source.MinimumRequired,
                SustainCostBySourceCount = source.SustainCostBySourceCount != null
                    ? new List<ExpressionSustainCostBySourceCountConfig>(source.SustainCostBySourceCount)
                    : new List<ExpressionSustainCostBySourceCountConfig>()
            };
        }

        /// <summary>复制表现引用配置。</summary>
        private static ExpressionPresentationConfig ClonePresentation(ExpressionPresentationConfig source)
        {
            if (source == null)
            {
                return null;
            }

            return new ExpressionPresentationConfig
            {
                ManualEntryIconTexPath = source.ManualEntryIconTexPath,
                VisualPresetDefName = source.VisualPresetDefName,
                VisualGraphicOverrideDefName = source.VisualGraphicOverrideDefName,
                CompositeVisualPresetDefName = source.CompositeVisualPresetDefName,
                ForceSuppressHostEquipment = source.ForceSuppressHostEquipment,
                VisualPriority = source.VisualPriority
            };
        }

        /// <summary>复制 VerbProps 字段级覆盖层。</summary>
        private static VerbPropsOverlay CloneVerbProps(VerbPropsOverlay source)
        {
            if (source == null)
            {
                return null;
            }

            return new VerbPropsOverlay
            {
                defaultProjectile = source.defaultProjectile,
                verbClass = source.verbClass,
                label = source.label,
                hasStandardCommand = source.hasStandardCommand,
                soundCast = source.soundCast,
                range = source.range,
                warmupTime = source.warmupTime,
                defaultCooldownTime = source.defaultCooldownTime,
                burstShotCount = source.burstShotCount,
                ticksBetweenBurstShots = source.ticksBetweenBurstShots,
                minRange = source.minRange,
                forcedMissRadius = source.forcedMissRadius,
                accuracyTouch = source.accuracyTouch,
                accuracyShort = source.accuracyShort,
                accuracyMedium = source.accuracyMedium,
                accuracyLong = source.accuracyLong,
                targetParams = source.targetParams
            };
        }

        /// <summary>复制组合 VerbProps 自动求值声明。</summary>
        private static ComboVerbPropsResolutionConfig CloneVerbPropsResolve(
            ComboVerbPropsResolutionConfig source)
        {
            if (source == null)
            {
                return null;
            }

            return new ComboVerbPropsResolutionConfig
            {
                RangeResolve = source.RangeResolve,
                WarmupTimeResolve = source.WarmupTimeResolve,
                BurstShotCountResolve = source.BurstShotCountResolve,
                TicksBetweenBurstShotsResolve = source.TicksBetweenBurstShotsResolve,
                MinRangeResolve = source.MinRangeResolve,
                ForcedMissRadiusResolve = source.ForcedMissRadiusResolve,
                AccuracyTouchResolve = source.AccuracyTouchResolve,
                AccuracyShortResolve = source.AccuracyShortResolve,
                AccuracyMediumResolve = source.AccuracyMediumResolve,
                AccuracyLongResolve = source.AccuracyLongResolve,
                DefaultCooldownTimeResolve = source.DefaultCooldownTimeResolve
            };
        }

        /// <summary>复制作者侧攻击执行配置。</summary>
        private static ChipAttackExecutionConfig CloneExecution(ChipAttackExecutionConfig source)
        {
            if (source == null)
            {
                return null;
            }

            return new ChipAttackExecutionConfig
            {
                Rhythm = source.Rhythm,
                HitCount = source.HitCount,
                HitIntervalTicks = source.HitIntervalTicks,
                OriginSpread = source.OriginSpread != null
                    ? new ChipAttackOriginSpreadConfig
                    {
                        LateralMin = source.OriginSpread.LateralMin,
                        LateralMax = source.OriginSpread.LateralMax,
                        ForwardMin = source.OriginSpread.ForwardMin,
                        ForwardMax = source.OriginSpread.ForwardMax
                    }
                    : null
            };
        }

        /// <summary>复制执行节奏自动求值声明。</summary>
        private static ComboExecutionResolutionConfig CloneExecutionResolve(
            ComboExecutionResolutionConfig source)
        {
            if (source == null)
            {
                return null;
            }

            return new ComboExecutionResolutionConfig
            {
                HitCountResolve = source.HitCountResolve,
                HitIntervalTicksResolve = source.HitIntervalTicksResolve,
                RhythmResolve = source.RhythmResolve
            };
        }

        /// <summary>复制 Trion 自动求值声明。</summary>
        private static ComboExpressionTrionResolutionConfig CloneTrionResolve(
            ComboExpressionTrionResolutionConfig source)
        {
            if (source == null)
            {
                return null;
            }

            return new ComboExpressionTrionResolutionConfig
            {
                UseCostResolve = source.UseCostResolve,
                MinimumRequiredResolve = source.MinimumRequiredResolve
            };
        }

        /// <summary>复制被动附加数据列表。</summary>
        private static List<PassiveExpressionExposedDatumConfig> CloneExposedData(
            IReadOnlyList<PassiveExpressionExposedDatumConfig> source)
        {
            List<PassiveExpressionExposedDatumConfig> result = new List<PassiveExpressionExposedDatumConfig>();
            if (source == null)
            {
                return result;
            }

            for (int index = 0; index < source.Count; index++)
            {
                PassiveExpressionExposedDatumConfig datum = source[index];
                if (datum != null)
                {
                    result.Add(new PassiveExpressionExposedDatumConfig
                    {
                        DataKey = datum.DataKey,
                        DataValue = datum.DataValue
                    });
                }
            }

            return result;
        }

        /// <summary>复制远程模块挂载列表。</summary>
        private static List<RangedModuleMountConfig> CloneRangedModules(
            IReadOnlyList<RangedModuleMountConfig> source)
        {
            List<RangedModuleMountConfig> result = new List<RangedModuleMountConfig>();
            if (source == null)
            {
                return result;
            }

            for (int index = 0; index < source.Count; index++)
            {
                RangedModuleMountConfig module = source[index];
                if (module != null)
                {
                    result.Add(module.Clone());
                }
            }

            return result;
        }
    }
}
