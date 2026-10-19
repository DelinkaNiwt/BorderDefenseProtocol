using System;
using System.Collections.Generic;
using BDP.Core.AttackExecution;
using BDP.Core.Chips;
using BDP.Core.CombatModel;
using BDP.Core.Combos;
using BDP.Core.Expressions;
using Verse;

namespace BDP.Core.Information
{
    /// <summary>
    /// 在正式发布时把组合最终结果投影到通用芯片功能树。
    /// 这里仅读取已求值结果，不重新解析 Combo 作者声明或参与组合执行。
    /// </summary>
    internal static class ComboFeatureInformationSnapshotBuilder
    {
        /// <summary>按正式结果 Id 构建组合功能树索引。</summary>
        internal static IReadOnlyDictionary<string, ChipFeatureResolutionResult> Build(
            ExpressionSnapshot snapshot)
        {
            Dictionary<string, ChipFeatureResolutionResult> result =
                new Dictionary<string, ChipFeatureResolutionResult>(StringComparer.Ordinal);
            IReadOnlyList<FormalExpressionResult> results = snapshot?.Results;
            if (results == null)
            {
                return result;
            }

            for (int index = 0; index < results.Count; index++)
            {
                FormalExpressionResult formalResult = results[index];
                if (formalResult == null
                    || formalResult.CompositeKind != CompositeExpressionKind.Combo
                    || string.IsNullOrWhiteSpace(formalResult.ComboDefName)
                    || string.IsNullOrWhiteSpace(formalResult.Id))
                {
                    continue;
                }

                ComboDef comboDef = DefDatabase<ComboDef>.GetNamedSilentFail(
                    formalResult.ComboDefName);
                if (comboDef == null)
                {
                    continue;
                }

                result[formalResult.Id] = Resolve(comboDef, formalResult);
            }

            return result;
        }

        /// <summary>用正式结果字段构造只读来源上下文并调用统一功能解析器。</summary>
        private static ChipFeatureResolutionResult Resolve(
            ComboDef comboDef,
            FormalExpressionResult formalResult)
        {
            List<ChipInformationDiagnostic> diagnostics =
                new List<ChipInformationDiagnostic>();
            if (string.IsNullOrWhiteSpace(formalResult.SourceEntryId))
            {
                diagnostics.Add(new ChipInformationDiagnostic(
                    "combo.feature.source_entry_missing",
                    formalResult.Id,
                    null,
                    ChipInformationDiagnosticSeverity.Error,
                    "正式组合结果未保留来源条目 Id，无法可靠解析其功能信息。"));
                return new ChipFeatureResolutionResult(null, diagnostics);
            }

            ChipExpressionEntryConfig finalEntry = CreateFinalEntry(formalResult);
            ChipDefinitionConfig finalConfig = new ChipDefinitionConfig
            {
                Expression = new ChipExpressionConfig
                {
                    Entries = new List<ChipExpressionEntryConfig> { finalEntry }
                },
                Features = comboDef.Features
            };
            diagnostics.AddRange(ChipFeatureDefinitionValidator.Validate(finalConfig));

            ChipFeatureResolutionResult resolved = new ChipFeatureResolver().Resolve(
                comboDef.Features,
                new ChipFeatureReadContext(
                    new ChipInformationIdentity(
                        formalResult.Id,
                        comboDef.defName,
                        null),
                    finalConfig,
                    finalConfig,
                    new ChipInformationRuntimeState(
                        true,
                        formalResult.IsAvailable,
                        formalResult.ModeKey,
                        null),
                    new[] { formalResult.SourceEntryId }));
            diagnostics.AddRange(resolved.Diagnostics);
            return new ChipFeatureResolutionResult(resolved.Features, diagnostics);
        }

        /// <summary>把已求值组合字段投影为来源 Worker 可读的条目；发布与声明读取共用此转换。</summary>
        internal static ChipExpressionEntryConfig CreateFinalEntry(
            FormalExpressionResult result)
        {
            return new ChipExpressionEntryConfig
            {
                Id = result.SourceEntryId,
                EntryType = MapEntryType(result),
                WeaponMode = MapWeaponMode(result.WeaponMode),
                DisplayLabel = result.DisplayLabel,
                VerbProps = result.VerbProps,
                Tool = result.Tool,
                tools = Copy(result.DeclaredTools),
                Maneuver = result.Maneuver,
                Execution = CreateExecution(result),
                AbilityDefName = result.AbilityDefName,
                HediffDefName = result.HediffDefName,
                HediffApplyModeKey = result.HediffApplyModeKey,
                HediffControlMode = result.HediffControlMode,
                PassiveIdentityKey = result.PassiveIdentityKey,
                ExposedData = CopyExposedData(result.ExposedData),
                RangedModules = CopyRangedModules(result.RangedModules),
                HitPartPolicy = result.HitPartPolicy,
                RangedModuleAugmentations = Copy(result.RangedModuleAugmentations),
                ProjectileOverrides = result.ResolvedVerbSpec != null
                    ? result.ResolvedVerbSpec.ProjectileOverrides
                    : null,
                Trion = CopyTrion(result.Trion)
            };
        }

        /// <summary>把已成立输出通道映射为现有条目种类。</summary>
        private static ChipExpressionEntryTypeConfig MapEntryType(FormalExpressionResult result)
        {
            switch (result.OutputChannel)
            {
                case ExpressionOutputChannel.Ability:
                    return ChipExpressionEntryTypeConfig.Ability;
                case ExpressionOutputChannel.Hediff:
                    return ChipExpressionEntryTypeConfig.Hediff;
                case ExpressionOutputChannel.Passive:
                    return ChipExpressionEntryTypeConfig.Passive;
                default:
                    return result.VerbAttackRole == VerbAttackRole.Secondary
                        ? ChipExpressionEntryTypeConfig.SecondaryVerb
                        : ChipExpressionEntryTypeConfig.PrimaryVerb;
            }
        }

        /// <summary>把已成立远近战模式映射为来源读取契约。</summary>
        private static VerbExpressionModeConfig MapWeaponMode(WeaponExpressionMode mode)
        {
            switch (mode)
            {
                case WeaponExpressionMode.Melee:
                    return VerbExpressionModeConfig.Melee;
                case WeaponExpressionMode.Ranged:
                    return VerbExpressionModeConfig.Ranged;
                default:
                    return VerbExpressionModeConfig.None;
            }
        }

        /// <summary>从最终执行风格创建来源 Worker 所需的简化执行字段。</summary>
        private static ChipAttackExecutionConfig CreateExecution(FormalExpressionResult result)
        {
            SingleAttackExecutionStyle style = result.ExecutionStyle?.Single;
            if (style == null)
            {
                return null;
            }

            ChipAttackExecutionConfig execution = new ChipAttackExecutionConfig();
            if (result.WeaponMode == WeaponExpressionMode.Ranged)
            {
                execution.Rhythm = style.RangedRhythm == RangedExecutionRhythm.Simultaneous
                    ? ChipAttackExecutionRhythmConfig.Simultaneous
                    : style.RangedRhythm == RangedExecutionRhythm.Sequential
                        ? ChipAttackExecutionRhythmConfig.Sequential
                        : ChipAttackExecutionRhythmConfig.None;
                execution.HitCount = result.VerbProps != null
                    ? result.VerbProps.burstShotCount
                    : 0;
                return execution;
            }

            if (result.WeaponMode == WeaponExpressionMode.Melee)
            {
                execution.Rhythm = ChipAttackExecutionRhythmConfig.Normal;
                execution.HitCount = style.meleeHitCount;
                execution.HitIntervalTicks = style.meleeHitIntervalTicks;
                return execution;
            }

            return null;
        }

        /// <summary>复制正式 Trion 结果，防止临时配置持有可变列表引用。</summary>
        private static ExpressionSourceTrionConfig CopyTrion(ExpressionSourceTrionConfig source)
        {
            if (source == null)
            {
                return null;
            }

            ExpressionSourceTrionConfig result = new ExpressionSourceTrionConfig
            {
                UseCost = source.UseCost,
                MinimumRequired = source.MinimumRequired
            };
            if (source.SustainCostBySourceCount != null)
            {
                for (int index = 0; index < source.SustainCostBySourceCount.Count; index++)
                {
                    ExpressionSustainCostBySourceCountConfig row =
                        source.SustainCostBySourceCount[index];
                    if (row != null)
                    {
                        result.SustainCostBySourceCount.Add(new ExpressionSustainCostBySourceCountConfig
                        {
                            SourceCount = row.SourceCount,
                            TotalPerSecond = row.TotalPerSecond
                        });
                    }
                }
            }

            return result;
        }

        /// <summary>复制组合正式结果暴露数据为条目配置形态。</summary>
        private static List<PassiveExpressionExposedDatumConfig> CopyExposedData(
            IReadOnlyList<PassiveExpressionExposedDatum> source)
        {
            List<PassiveExpressionExposedDatumConfig> result =
                new List<PassiveExpressionExposedDatumConfig>();
            if (source == null)
            {
                return result;
            }

            for (int index = 0; index < source.Count; index++)
            {
                PassiveExpressionExposedDatum datum = source[index];
                if (datum != null)
                {
                    result.Add(new PassiveExpressionExposedDatumConfig
                    {
                        DataKey = datum.Key,
                        DataValue = datum.Value
                    });
                }
            }

            return result;
        }

        /// <summary>复制正式远程模块快照，保留最终启用状态和来源隔离键。</summary>
        private static List<RangedModuleMountConfig> CopyRangedModules(
            IReadOnlyList<RangedModuleMountConfig> source)
        {
            List<RangedModuleMountConfig> result = new List<RangedModuleMountConfig>();
            if (source != null)
            {
                for (int index = 0; index < source.Count; index++)
                {
                    if (source[index] != null)
                    {
                        result.Add(source[index].Clone());
                    }
                }
            }

            return result;
        }

        /// <summary>浅复制来源 Worker 读取但不会改写的正式对象列表。</summary>
        private static List<T> Copy<T>(IReadOnlyList<T> source)
            where T : class
        {
            List<T> result = new List<T>();
            if (source != null)
            {
                for (int index = 0; index < source.Count; index++)
                {
                    if (source[index] != null)
                    {
                        result.Add(source[index]);
                    }
                }
            }

            return result;
        }
    }
}
