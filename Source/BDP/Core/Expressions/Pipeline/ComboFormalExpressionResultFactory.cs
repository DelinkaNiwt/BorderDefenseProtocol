using System.Collections.Generic;
using BDP.Core.AttackExecution;
using BDP.Core.CombatModel;
using BDP.Core.Combos;
using BDP.Core.Semantics;
using RimWorld;
using Verse;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// Combo 正式结果工厂。
    /// 它统一承接 Combo 条目到正式结果的拼装，不让 Composite resolver 继续内联各字段的临时回退。
    /// </summary>
    internal sealed class ComboFormalExpressionResultFactory
    {
        /// <summary>
        /// 根据一份已解析的 Combo 输入构建正式结果。
        /// </summary>
        internal FormalExpressionResult Build(ComboFormalExpressionResolution resolution)
        {
            ComboDefinitionReadResult comboReadResult = resolution != null ? resolution.ComboReadResult : null;
            ChipExpressionEntryContract entry = resolution != null ? resolution.EntryContract : null;
            if (comboReadResult == null || comboReadResult.ComboDef == null || entry == null)
            {
                return null;
            }

            ExpressionResultKind resultKind = TranslateResultKind(entry.Kind);
            WeaponExpressionMode weaponMode = TranslateWeaponMode(entry.WeaponMode);
            FormalExpressionResult mainSourceResult = resolution.MainSourceResult;
            FormalExpressionResult subSourceResult = resolution.SubSourceResult;
            // 基底：Main 侧完整 VerbProps / ResolvedVerbSpec。
            // 条目侧是 VerbPropsOverlay（增量），类型不兼容 VerbProperties，
            // 无法被误当基底——从结构上排除了旧 bug。
            VerbProperties fallbackVerbProps = (mainSourceResult != null ? mainSourceResult.VerbProps : null)
                ?? (subSourceResult != null ? subSourceResult.VerbProps : null);
            ResolvedVerbSpec fallbackVerbSpec = (mainSourceResult != null ? mainSourceResult.ResolvedVerbSpec : null)
                ?? (subSourceResult != null ? subSourceResult.ResolvedVerbSpec : null);
            ResolvedVerbSpec resolvedVerbSpec = resultKind == ExpressionResultKind.Verb
                ? ResolvedVerbSpecFactory.ResolveComboSpec(
                    fallbackVerbProps,
                    fallbackVerbSpec,
                    resolution.ResolvedVerbProps,
                    entry.ResolvedVerbSpec != null
                        ? entry.ResolvedVerbSpec.ProjectileOverrides
                        : null)
                : null;
            ExpressionSourceTrionConfig resolvedTrion = ResolveResultTrion(resolution);

            return new FormalExpressionResult
            {
                Id = BuildComboResultId(comboReadResult.ComboDef.defName, entry.Id),
                ResultKind = resultKind,
                WeaponMode = weaponMode,
                OriginKind = ExpressionOriginKind.Composite,
                CompositeKind = CompositeExpressionKind.Combo,
                ComboDefName = comboReadResult.ComboDef.defName,
                SourceVariantKey = resolution.SourceVariantKey,
                SourceVariantLabel = resolution.SourceVariantLabel,
                DisplayLabel = !string.IsNullOrWhiteSpace(entry.DisplayLabel)
                    ? entry.DisplayLabel
                    : comboReadResult.ComboDef.label,
                ManualEntryIconTexPath = entry.ManualEntryIconTexPath,
                VisualPresetDefName = entry.VisualPresetDefName,
                VisualGraphicOverrideDefName = entry.VisualGraphicOverrideDefName,
                CompositeVisualPresetDefName = entry.CompositeVisualPresetDefName,
                ForceSuppressHostEquipment = entry.ForceSuppressHostEquipment,
                VisualPriority = entry.VisualPriority,
                ManualEntryAggregationKey = BuildComboAggregationKey(comboReadResult.ComboDef.defName, entry.Id),
                RoleKey = entry.RoleKey,
                VerbAttackRole = entry.VerbAttackRole,
                Tags = entry.Tags != null ? new List<string>(entry.Tags) : new List<string>(),
                ExecutionSlotKey = BuildComboExecutionSlotKey(entry, weaponMode),
                IsSecondaryAttack = entry.VerbAttackRole == VerbAttackRole.Secondary,
                Trion = resolvedTrion,
                IsAvailable = true,
                UseRequirementCheck = resolution.UseRequirementCheck,
                CanProject = true,
                SemanticContext = BuildComboSemanticContext(comboReadResult.ComboDef, entry),
                ModeKey = entry.ModeKey,
                ExecutionStyle = ResolveComboExecutionStyle(resolution),
                VerbProps = resultKind == ExpressionResultKind.Verb
                    ? ResolvedVerbSpecFactory.CreateSurfaceVerbProps(resolvedVerbSpec)
                    : null,
                ResolvedVerbSpec = resolvedVerbSpec,
                Tool = entry.Tool,
                DeclaredTools = entry.DeclaredTools != null ? new List<Tool>(entry.DeclaredTools) : new List<Tool>(),
                DeclaredMeleeToolSurfaces = CloneMeleeToolSurfaces(entry.DeclaredMeleeToolSurfaces),
                Maneuver = entry.Maneuver,
                AbilityDefName = entry.AbilityDefName,
                HediffDefName = entry.HediffDefName,
                HediffApplyModeKey = entry.HediffApplyModeKey,
                PassiveKey = entry.PassiveKey,
                ExposedData = TranslateComboExposedData(entry.ExposedData),
                RangedModules = entry.RangedModules != null ? CloneRangedModules(entry.RangedModules) : new List<RangedModuleMountConfig>()
            };
        }

        /// <summary>
        /// 为组合技结果构建稳定结果标识。
        /// </summary>
        private static string BuildComboResultId(string comboDefName, string entryId)
        {
            string safeComboDefName = !string.IsNullOrWhiteSpace(comboDefName) ? comboDefName : "combo";
            string safeEntryId = !string.IsNullOrWhiteSpace(entryId) ? entryId : "entry";
            return "combo:" + safeComboDefName + ":" + safeEntryId;
        }

        /// <summary>
        /// 为组合技入口构建稳定的手动聚合键。
        /// </summary>
        private static string BuildComboAggregationKey(string comboDefName, string entryId)
        {
            string safeComboDefName = !string.IsNullOrWhiteSpace(comboDefName) ? comboDefName : "combo";
            string safeEntryId = !string.IsNullOrWhiteSpace(entryId) ? entryId : "entry";
            return "combo:" + safeComboDefName + ":" + safeEntryId;
        }

        /// <summary>
        /// 为组合技结果构建执行槽位键。
        /// 当前组合技仍作为普通单结果消费，不另开新的宿主槽体系。
        /// </summary>
        private static string BuildComboExecutionSlotKey(
            ChipExpressionEntryContract entry,
            WeaponExpressionMode weaponMode)
        {
            if (entry == null || TranslateResultKind(entry.Kind) != ExpressionResultKind.Verb)
            {
                return null;
            }

            return weaponMode == WeaponExpressionMode.Melee
                ? "ComboMeleePrimary"
                : "ComboPrimary";
        }

        /// <summary>
        /// 为组合技结果构建最小语义上下文。
        /// </summary>
        private static ISemanticContext BuildComboSemanticContext(ComboDef comboDef, ChipExpressionEntryContract entry)
        {
            return new SemanticContext
            {
                Id = BuildComboResultId(comboDef != null ? comboDef.defName : null, entry != null ? entry.Id : null),
                DisplayLabel = !string.IsNullOrWhiteSpace(entry?.DisplayLabel)
                    ? entry.DisplayLabel
                    : comboDef != null ? comboDef.label : null,
                SourceKind = entry != null ? entry.SemanticSourceKind : SemanticSourceKind.Unknown
            };
        }

        /// <summary>
        /// 把组合技作者声明的被动附加数据翻译成正式结果数据。
        /// </summary>
        private static IReadOnlyList<PassiveExpressionExposedDatum> TranslateComboExposedData(
            List<PassiveExpressionExposedDatumConfig> configs)
        {
            List<PassiveExpressionExposedDatum> result = new List<PassiveExpressionExposedDatum>();
            if (configs == null)
            {
                return result;
            }

            for (int i = 0; i < configs.Count; i++)
            {
                PassiveExpressionExposedDatumConfig config = configs[i];
                if (config == null || string.IsNullOrWhiteSpace(config.DataKey))
                {
                    continue;
                }

                result.Add(new PassiveExpressionExposedDatum
                {
                    Key = config.DataKey,
                    Value = config.DataValue
                });
            }

            return result;
        }

        /// <summary>
        /// 解析组合技结果应采用的正式执行风格。
        /// 基底优先级：Main 侧 > Sub 侧 > 条目显式。
        /// 条目和 ExecutionResolve 求值结果作为 overlay 叠加到基底上。
        /// </summary>
        private static AttackExecutionStyle ResolveComboExecutionStyle(ComboFormalExpressionResolution resolution)
        {
            ChipExpressionEntryContract entry = resolution != null ? resolution.EntryContract : null;
            FormalExpressionResult mainSourceResult = resolution != null ? resolution.MainSourceResult : null;
            FormalExpressionResult subSourceResult = resolution != null ? resolution.SubSourceResult : null;
            ComboResolvedExecution resolvedExecution = resolution != null ? resolution.ResolvedExecution : null;

            // 基底：Main > Sub > 条目
            AttackExecutionStyle baseStyle = (mainSourceResult?.ExecutionStyle?.Clone())
                ?? (subSourceResult?.ExecutionStyle?.Clone())
                ?? (entry?.ExecutionStyle?.Clone());

            if (baseStyle != null)
            {
                ApplyResolvedMeleeExecution(baseStyle, resolvedExecution);
                ApplyResolvedRangedRhythm(baseStyle, resolvedExecution);
                return baseStyle;
            }

            // 无任何基底：近战条目回退到解析结果构造最小风格
            if (entry != null && TranslateWeaponMode(entry.WeaponMode) == WeaponExpressionMode.Melee && resolvedExecution != null)
            {
                int hitCount = resolvedExecution.HitCount != null && resolvedExecution.HitCount.HasResolvedValue
                    ? resolvedExecution.HitCount.ResolvedValue
                    : 1;
                int hitIntervalTicks = resolvedExecution.HitIntervalTicks != null && resolvedExecution.HitIntervalTicks.HasResolvedValue
                    ? resolvedExecution.HitIntervalTicks.ResolvedValue
                    : 0;
                return new AttackExecutionStyle
                {
                    Single = new SingleAttackExecutionStyle
                    {
                        MeleeRhythm = hitCount > 1
                            ? MeleeExecutionRhythm.MultiHit
                            : MeleeExecutionRhythm.SingleHit,
                        meleeHitCount = hitCount,
                        meleeHitIntervalTicks = hitIntervalTicks
                    }
                };
            }

            return null;
        }

        /// <summary>
        /// 把组合技求值后的近战节奏补到现有执行风格上。
        /// 只有近战字段在这里被覆盖，远程节奏仍沿用当前兼容规则。
        /// </summary>
        private static void ApplyResolvedMeleeExecution(
            AttackExecutionStyle style,
            ComboResolvedExecution resolvedExecution)
        {
            if (style?.Single == null || resolvedExecution == null)
            {
                return;
            }

            if (resolvedExecution.HitCount != null && resolvedExecution.HitCount.HasResolvedValue)
            {
                style.Single.meleeHitCount = resolvedExecution.HitCount.ResolvedValue;
                style.Single.MeleeRhythm = style.Single.meleeHitCount > 1
                    ? MeleeExecutionRhythm.MultiHit
                    : MeleeExecutionRhythm.SingleHit;
            }

            if (resolvedExecution.HitIntervalTicks != null && resolvedExecution.HitIntervalTicks.HasResolvedValue)
            {
                style.Single.meleeHitIntervalTicks = resolvedExecution.HitIntervalTicks.ResolvedValue;
            }
        }

        /// <summary>
        /// 解析结果级 Trion 字段。
        /// 显式条目优先，其次按 Combo 的 TrionResolve 从来源材料求值。
        /// 两者都未声明时不收费，不再猜测来源芯片的费用。
        /// </summary>
        private static ExpressionSourceTrionConfig ResolveResultTrion(
            ComboFormalExpressionResolution resolution)
        {
            ChipExpressionEntryContract entry = resolution != null ? resolution.EntryContract : null;
            if (entry != null && entry.Trion != null)
            {
                return CloneTrion(entry.Trion);
            }

            ComboExpressionTrionResolutionConfig trionResolve = resolution != null && resolution.EntryConfig != null
                ? resolution.EntryConfig.TrionResolve
                : null;
            ExpressionSourceTrionConfig resolved = ResolveTrionFromSourceMaterials(resolution, trionResolve);
            if (resolved != null)
            {
                return resolved;
            }

            return null;
        }

        /// <summary>
        /// 按 Combo TrionResolve 从两侧来源材料计算表达级 Trion。
        /// </summary>
        private static ExpressionSourceTrionConfig ResolveTrionFromSourceMaterials(
            ComboFormalExpressionResolution resolution,
            ComboExpressionTrionResolutionConfig trionResolve)
        {
            if (resolution == null || trionResolve == null)
            {
                return null;
            }

            ExpressionSourceTrionConfig mainTrion = resolution.MainSourceMaterial != null
                ? resolution.MainSourceMaterial.Trion
                : null;
            ExpressionSourceTrionConfig subTrion = resolution.SubSourceMaterial != null
                ? resolution.SubSourceMaterial.Trion
                : null;

            ComboResolvedFieldValue<float> use = ComboSourceFieldResolver.ResolveFloat(
                null,
                trionResolve.UseCostResolve,
                mainTrion != null ? mainTrion.UseCost : 0f,
                subTrion != null ? subTrion.UseCost : 0f);
            ComboResolvedFieldValue<float> minimum = ComboSourceFieldResolver.ResolveFloat(
                null,
                trionResolve.MinimumRequiredResolve,
                mainTrion != null ? mainTrion.MinimumRequired : 0f,
                subTrion != null ? subTrion.MinimumRequired : 0f);

            if (!use.HasResolvedValue
                && !minimum.HasResolvedValue)
            {
                return null;
            }

            return new ExpressionSourceTrionConfig
            {
                UseCost = use.HasResolvedValue ? use.ResolvedValue : 0f,
                MinimumRequired = minimum.HasResolvedValue ? minimum.ResolvedValue : 0f
            };
        }

        /// <summary>
        /// 克隆表达级 Trion 配置。
        /// </summary>
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
                SustainCostBySourceCount = CloneSustainCostBySourceCount(source.SustainCostBySourceCount)
            };
        }

        /// <summary>
        /// 深复制组合技显式声明的持续费用表。
        /// TrionResolve 不调用本方法，因此不会从来源芯片隐式继承该表。
        /// </summary>
        private static List<ExpressionSustainCostBySourceCountConfig> CloneSustainCostBySourceCount(
            IReadOnlyList<ExpressionSustainCostBySourceCountConfig> source)
        {
            List<ExpressionSustainCostBySourceCountConfig> result =
                new List<ExpressionSustainCostBySourceCountConfig>();
            if (source == null)
            {
                return result;
            }

            for (int i = 0; i < source.Count; i++)
            {
                ExpressionSustainCostBySourceCountConfig row = source[i];
                if (row == null)
                {
                    continue;
                }

                result.Add(new ExpressionSustainCostBySourceCountConfig
                {
                    SourceCount = row.SourceCount,
                    TotalPerSecond = row.TotalPerSecond
                });
            }

            return result;
        }

        /// <summary>
        /// 把表达条目种类翻译成正式结果种类。
        /// </summary>
        private static ExpressionResultKind TranslateResultKind(ChipExpressionEntryKind kind)
        {
            switch (kind)
            {
                case ChipExpressionEntryKind.PrimaryVerb:
                case ChipExpressionEntryKind.SecondaryVerb:
                    return ExpressionResultKind.Verb;
                case ChipExpressionEntryKind.Ability:
                    return ExpressionResultKind.Ability;
                case ChipExpressionEntryKind.Hediff:
                    return ExpressionResultKind.Hediff;
                case ChipExpressionEntryKind.Passive:
                    return ExpressionResultKind.Passive;
                default:
                    return ExpressionResultKind.Verb;
            }
        }

        /// <summary>
        /// 把配置层武器模式翻译成正式结果武器模式。
        /// </summary>
        private static WeaponExpressionMode TranslateWeaponMode(VerbExpressionModeConfig configMode)
        {
            switch (configMode)
            {
                case VerbExpressionModeConfig.Melee:
                    return WeaponExpressionMode.Melee;
                case VerbExpressionModeConfig.Ranged:
                    return WeaponExpressionMode.Ranged;
                default:
                    return WeaponExpressionMode.None;
            }
        }

        /// <summary>
        /// 把组合技求值后的远程射击节奏补到现有执行风格上。
        /// 只有远程 Rhythm 字段在这里被覆盖，近战节奏不受影响。
        /// </summary>
        private static void ApplyResolvedRangedRhythm(
            AttackExecutionStyle style,
            ComboResolvedExecution resolvedExecution)
        {
            if (style?.Single == null || resolvedExecution?.Rhythm == null)
            {
                return;
            }

            if (resolvedExecution.Rhythm.HasResolvedValue)
            {
                style.Single.RangedRhythm = resolvedExecution.Rhythm.ResolvedValue;
            }
        }

        /// <summary>
        /// 对近战 Tool 表面做最小浅复制，避免共享可变引用。
        /// </summary>
        private static IReadOnlyList<MeleeToolSurface> CloneMeleeToolSurfaces(
            IReadOnlyList<MeleeToolSurface> surfaces)
        {
            List<MeleeToolSurface> result = new List<MeleeToolSurface>();
            if (surfaces == null)
            {
                return result;
            }

            for (int i = 0; i < surfaces.Count; i++)
            {
                MeleeToolSurface surface = surfaces[i];
                if (surface == null)
                {
                    continue;
                }

                result.Add(new MeleeToolSurface
                {
                    Tool = surface.Tool,
                    VerbProps = surface.VerbProps,
                    Maneuver = surface.Maneuver,
                    DamageDef = surface.DamageDef,
                    DeclaredIndex = surface.DeclaredIndex
                });
            }

            return result;
        }

        /// <summary>
        /// 对模块挂载快照做最小复制，避免组合结果回写来源结果。
        /// </summary>
        private static IReadOnlyList<RangedModuleMountConfig> CloneRangedModules(
            IReadOnlyList<RangedModuleMountConfig> modules)
        {
            List<RangedModuleMountConfig> result = new List<RangedModuleMountConfig>();
            if (modules == null)
            {
                return result;
            }

            for (int i = 0; i < modules.Count; i++)
            {
                RangedModuleMountConfig module = modules[i];
                if (module == null)
                {
                    continue;
                }

                result.Add(module.Clone());
            }

            return result;
        }
    }
}
