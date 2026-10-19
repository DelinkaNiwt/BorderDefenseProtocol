using System;
using System.Collections.Generic;
using BDP.Core.Chips;
using BDP.Core.CombatModel;
using BDP.Core.Expressions;
using BDP.Core.Expressions.External;
using BDP.Core.Expressions.Runtime;
using Verse;
using BDP.Core.Information;

namespace BDP.Core.Combos
{
    /// <summary>把组合声明与两侧声明配置求值为来源条目；不读取实例、发布状态或执行战斗。</summary>
    public static class ComboDeclarationResolver
    {
        /// <summary>复用正式字段求值、结果工厂和信息条目转换，保留声明中的来源跟随规则。</summary>
        public static ChipExpressionConfig ResolveEntries(
            ComboDef combo, ChipDefinitionConfig firstSource, ChipDefinitionConfig secondSource,
            ChipSourceReferenceSnapshot firstReference, ChipSourceReferenceSnapshot secondReference)
        {
            return ResolveEntries(
                combo,
                firstSource,
                secondSource,
                firstReference,
                secondReference,
                true);
        }

        /// <summary>
        /// 按正式来源与准入规则求组合结果，但不应用来源变体对组合输出的修正。
        /// 用于比较进入组合前后的结果；来源配置本身仍保留其已有修正。
        /// </summary>
        public static ChipExpressionConfig ResolveEntriesWithoutVariantModifiers(
            ComboDef combo, ChipDefinitionConfig firstSource, ChipDefinitionConfig secondSource,
            ChipSourceReferenceSnapshot firstReference, ChipSourceReferenceSnapshot secondReference)
        {
            return ResolveEntries(
                combo,
                firstSource,
                secondSource,
                firstReference,
                secondReference,
                false);
        }

        /// <summary>按正式解析顺序可选应用来源变体修正，并生成组合结果条目。</summary>
        private static ChipExpressionConfig ResolveEntries(
            ComboDef combo, ChipDefinitionConfig firstSource, ChipDefinitionConfig secondSource,
            ChipSourceReferenceSnapshot firstReference, ChipSourceReferenceSnapshot secondReference,
            bool applyVariantModifiers)
        {
            List<ChipExpressionEntryConfig> entries = new List<ChipExpressionEntryConfig>();
            if (combo?.Expression?.Entries == null
                || !IsPairAllowed(combo, firstSource, firstReference, secondSource, secondReference))
                return new ChipExpressionConfig { Entries = entries };

            ComboDefinitionReadResult read = ComboSurfaceAccess.Read(combo);
            ComboDefinitionContractResolver resolver = ComboSurfaceAccess.ResolveContractResolver();
            ComboFormalExpressionResultFactory factory = new ComboFormalExpressionResultFactory();
            // 两条路径共用同一份副本和正式求值；仅由调用方决定是否应用变体修正。
            List<ComboExpressionEntryConfig> comboEntries = ComboExpressionEntryCloneService.CloneEntries(
                combo.Expression.Entries);
            string sourceVariantKey = ComboSourceAdmissionEvaluator.AreSourceVariantsCompatible(
                ComboSourceAdmissionEvaluator.BuildSnapshot(firstSource, firstReference),
                ComboSourceAdmissionEvaluator.BuildSnapshot(secondSource, secondReference))
                ? firstReference.SourceVariantKey : null;
            if (applyVariantModifiers)
            {
                ComboExpressionVariantModifierRegistry.Apply(comboEntries, sourceVariantKey);
            }
            List<ChipExpressionEntryConfig> declarations = new List<ChipExpressionEntryConfig>();
            foreach (ComboExpressionEntryConfig declaration in comboEntries)
                if (declaration != null) declarations.Add(declaration.ToChipExpressionEntryConfig());
            // 整份声明一起解释，保留父子条目引用，不能逐条解释后丢失关联。
            ChipExpressionResolvedContract contract = ChipExpressionContractInterpreter.ResolveUncached(
                new ChipExpressionConfig { Entries = declarations }, null, null);
            foreach (ComboExpressionEntryConfig declaration in comboEntries)
            {
                if (declaration == null) continue;
                ChipExpressionEntryContract declared = FindEntry(contract.Contract?.Entries, declaration.Id);
                if (declared == null) continue;
                FormalExpressionResult first = ResolveSource(firstSource, combo.FirstSourceScope, out ExpressionSourceMaterial firstMaterial);
                FormalExpressionResult second = ResolveSource(secondSource, combo.SecondSourceScope, out ExpressionSourceMaterial secondMaterial);
                FormalExpressionResult result = factory.Build(new ComboFormalExpressionResolution
                {
                    ComboReadResult = read,
                    SourceVariantKey = sourceVariantKey,
                    EntryConfig = declaration,
                    EntryContract = declared,
                    FirstSourceResult = first,
                    SecondSourceResult = second,
                    FirstSourceMaterial = firstMaterial,
                    SecondSourceMaterial = secondMaterial,
                    ResolvedVerbProps = resolver.ResolveVerbProps(declaration, first, second),
                    ResolvedExecution = resolver.ResolveExecution(declaration, first, second)
                });
                if (result != null)
                {
                    ChipExpressionEntryConfig finalEntry = ComboFeatureInformationSnapshotBuilder.CreateFinalEntry(result);
                    // 能力引用本身是完整声明来源，保持 AbilityDef 基值并应用同一字段求值结果。
                    if (declaration.EntryType == ChipExpressionEntryTypeConfig.Ability)
                        finalEntry.VerbProps = ResolvedVerbSpecFactory.CreateSurfaceVerbProps(
                            ResolvedVerbSpecFactory.ApplyComboOverrides(
                                ResolvedVerbSpecFactory.FromAbilityDef(declaration.AbilityDefName, declaration.ProjectileOverrides),
                                resolver.ResolveVerbProps(declaration, first, second)));
                    entries.Add(finalEntry);
                }
            }
            return new ChipExpressionConfig { Entries = entries };
        }

        /// <summary>理论两侧按正式来源身份、单侧准入和同构型约束筛查，失败时不生成结果。</summary>
        public static bool IsPairAllowed(ComboDef combo,
            ChipDefinitionConfig firstSource, ChipSourceReferenceSnapshot firstReference,
            ChipDefinitionConfig secondSource, ChipSourceReferenceSnapshot secondReference)
        {
            if (!IsSourceAllowed(combo, firstSource, firstReference, true)
                || !IsSourceAllowed(combo, secondSource, secondReference, false)) return false;
            ComboDefinitionContract contract = ComboSurfaceAccess.Read(combo).Contract;
            ComboSourceAdmissionSnapshot first = BuildTheoreticalSnapshot(firstSource, firstReference, contract.FirstSourceScope);
            ComboSourceAdmissionSnapshot second = BuildTheoreticalSnapshot(secondSource, secondReference, contract.SecondSourceScope);
            string firstKey = firstReference.OrderedSourceKeys[0];
            string secondKey = secondReference.OrderedSourceKeys[0];
            if (!MatchesPair(combo, firstKey, secondKey, first, second)) return false;
            // 正式索引拒绝多份定义同时成立；理论入口也不能把歧义组合分别显示为合法结果。
            foreach (ComboDef candidate in DefDatabase<ComboDef>.AllDefsListForReading)
                if (candidate != combo && MatchesPair(candidate, firstKey, secondKey, first, second)) return false;
            return true;
        }

        /// <summary>复用正式索引的正反分配、单侧身份准入、范围与构型配对规则。</summary>
        private static bool MatchesPair(ComboDef combo, string firstKey, string secondKey,
            ComboSourceAdmissionSnapshot first, ComboSourceAdmissionSnapshot second)
        {
            ComboDefinitionReadResult read = ComboSurfaceAccess.Read(combo);
            if (read?.Validation?.IsValid != true || read.Contract == null) return false;
            ComboDefinitionContract c = read.Contract;
            return ComboRuntimeIndex.MatchesAssignmentWithScope(firstKey, secondKey, first, second,
                    c.FirstSourceKey, c.SecondSourceKey, c.FirstSourceAdmission, c.SecondSourceAdmission,
                    c.FirstSourceScope, c.SecondSourceScope, c.RequireSameSourceVariant)
                || ComboRuntimeIndex.MatchesAssignmentWithScope(firstKey, secondKey, first, second,
                    c.SecondSourceKey, c.FirstSourceKey, c.SecondSourceAdmission, c.FirstSourceAdmission,
                    c.SecondSourceScope, c.FirstSourceScope, c.RequireSameSourceVariant);
        }

        /// <summary>理论范围取当前关系的形态姿态，没有范围限制时沿用来源正式默认范围。</summary>
        private static ComboSourceAdmissionSnapshot BuildTheoreticalSnapshot(ChipDefinitionConfig config,
            ChipSourceReferenceSnapshot source, ComboSourceScopeContract scope)
        {
            ComboSourceAdmissionSnapshot result = ComboSourceAdmissionEvaluator.BuildSnapshot(config, source);
            result.ModeKey = scope?.ModeKey ?? config.Expression?.DefaultModeKey;
            result.StanceKey = scope?.StanceKey;
            if (result.StanceKey == null && config.Expression?.Modes != null)
                foreach (ChipExpressionModeConfig mode in config.Expression.Modes)
                    if (string.Equals(mode.ModeKey, result.ModeKey, StringComparison.OrdinalIgnoreCase))
                        result.StanceKey = mode.DefaultStanceKey;
            result.HasRuntimeState = true;
            return result;
        }

        /// <summary>匹配来源首键与成品职业/分类/标签/构型；指定形态姿态必须能被正式解释器解析。</summary>
        public static bool IsSourceAllowed(ComboDef combo, ChipDefinitionConfig config,
            ChipSourceReferenceSnapshot source, bool firstSide)
        {
            ComboDefinitionReadResult read = combo == null ? null : ComboSurfaceAccess.Read(combo);
            if (read?.Validation?.IsValid != true || read.Contract == null || config == null
                || source?.OrderedSourceKeys == null || source.OrderedSourceKeys.Count == 0) return false;
            string expectedKey = firstSide ? read.Contract.FirstSourceKey : read.Contract.SecondSourceKey;
            if (!string.Equals(source.OrderedSourceKeys[0], expectedKey, StringComparison.OrdinalIgnoreCase)
                || !ComboSourceAdmissionEvaluator.IsAllowed(ComboSourceAdmissionEvaluator.BuildSnapshot(config, source),
                    firstSide ? read.Contract.FirstSourceAdmission : read.Contract.SecondSourceAdmission)) return false;
            ComboSourceScopeContract scope = firstSide ? read.Contract.FirstSourceScope : read.Contract.SecondSourceScope;
            if (scope == null) return true;
            ChipExpressionResolvedContract expression = ChipExpressionContractInterpreter.ResolveUncached(
                config.Expression, scope.ModeKey, scope.StanceKey);
            return expression.Validation?.IsValid == true && expression.Contract?.Entries?.Count > 0;
        }

        /// <summary>按稳定条目标识找到完整声明解释后的对应条目。</summary>
        private static ChipExpressionEntryContract FindEntry(IReadOnlyList<ChipExpressionEntryContract> entries, string id)
        {
            if (entries != null)
                foreach (ChipExpressionEntryContract entry in entries)
                    if (entry.Id == id) return entry;
            return null;
        }

        /// <summary>按正式来源规则取主攻击；无主攻击时仅用首条独立来源读取费用，不把副攻击当主攻击。</summary>
        private static FormalExpressionResult ResolveSource(
            ChipDefinitionConfig source, ComboSourceScopeConfig scope, out ExpressionSourceMaterial material)
        {
            material = null;
            ChipExpressionResolvedContract contract = ChipExpressionContractInterpreter.ResolveUncached(
                source?.Expression, scope?.ModeKey, scope?.StanceKey);
            if (contract.Contract?.Entries == null) return null;
            ChipExpressionEntryContract reference = null;
            ChipExpressionEntryContract primary = null;
            string mode = scope?.ModeKey ?? contract.Contract.DefaultModeKey;
            foreach (ChipExpressionEntryContract entry in contract.Contract.Entries)
            {
                if ((entry.ModeKey != null && entry.ModeKey != mode)
                    || (scope?.StanceKey != null && entry.StanceKey != scope.StanceKey)) continue;
                if (reference == null) reference = entry;
                if (entry.EntryType == ChipExpressionEntryType.PrimaryVerb)
                { primary = entry; break; }
            }
            reference = primary ?? reference;
            if (reference != null) material = new ExpressionSourceMaterial { Trion = reference.Trion };
            return primary == null ? null : new FormalExpressionResult
            {
                VerbProps = primary.VerbProps,
                ResolvedVerbSpec = primary.ResolvedVerbSpec,
                ExecutionStyle = primary.ExecutionStyle,
                Trion = primary.Trion
            };
        }
    }
}
