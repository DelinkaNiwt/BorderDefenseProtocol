using System.Collections.Generic;
using BDP.Core.AttackExecution;
using BDP.Core.Combos;
using BDP.Core.CombatModel;
using BDP.Core.Expressions.External;
using BDP.Core.Semantics;
using BDP.Core.Trigger;
using BDP.Support.Diagnostics;
using Verse;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 复合表达结果解析器。
    /// 它负责把 Main / Sub 单侧结果重组为组合技与双持复合结果。
    /// </summary>
    internal sealed class CompositeExpressionResolver
    {
        /// <summary>
        /// 组合技条目翻译器。
        /// 当前继续复用芯片表达解释器，避免为组合技额外维护一条并行解释链。
        /// </summary>
        private static readonly IChipExpressionContractInterpreter comboEntryInterpreter =
            new ChipExpressionContractInterpreter();

        /// <summary>
        /// Combo 正式结果工厂。
        /// 它统一承接 Combo 条目的正式结果拼装。
        /// </summary>
        private static readonly ComboFormalExpressionResultFactory comboResultFactory =
            new ComboFormalExpressionResultFactory();

        /// <summary>
        /// 根据 Main / Sub 单侧结果重算复合结果集合。
        /// </summary>
        internal CompositeExpressionSet Resolve(
            Pawn pawn,
            SingleSideExpressionSet mainSet,
            SingleSideExpressionSet subSet,
            ITriggerLoadoutReader triggerLoadoutReader,
            IReadOnlyDictionary<string, ExpressionSourceMaterial> materialIndex,
            IReadOnlyList<ExpressionSourceMaterial> sourceMaterials)
        {
            List<FormalExpressionResult> dualWeaponResults = new List<FormalExpressionResult>();
            List<CompositeExpressionReference> references = new List<CompositeExpressionReference>();

            FormalExpressionResult mainPrimary = FindSingleSidePrimary(mainSet);
            FormalExpressionResult subPrimary = FindSingleSidePrimary(subSet);
            ExpressionSourceMaterial mainMaterial = ResolveSourceMaterial(sourceMaterials, TriggerSide.Main);
            ExpressionSourceMaterial subMaterial = ResolveSourceMaterial(sourceMaterials, TriggerSide.Sub);
            List<FormalExpressionResult> comboResults = BuildComboResults(
                pawn,
                triggerLoadoutReader,
                mainMaterial,
                subMaterial,
                mainPrimary,
                subPrimary,
                references);
            // 双持判定需要精确匹配 PrimaryVerb 对应的来源材料，不能取侧第一条。
            // 否则多形态芯片的 RuntimePayload.Entry.Id 可能来自非 Verb 条目，导致动作比较失准。
            ExpressionSourceMaterial mainPrimaryMaterial = ResolveMaterialForResult(sourceMaterials, materialIndex, mainPrimary);
            ExpressionSourceMaterial subPrimaryMaterial = ResolveMaterialForResult(sourceMaterials, materialIndex, subPrimary);
            FormalExpressionResult dualPrimary = BuildDualPrimaryResult(mainPrimary, subPrimary, mainPrimaryMaterial, subPrimaryMaterial, references);
            if (dualPrimary != null)
            {
                dualWeaponResults.Add(dualPrimary);
            }

            return new CompositeExpressionSet
            {
                DualWeaponResults = dualWeaponResults,
                ComboResults = comboResults,
                NonCombatCompositeResults = new List<FormalExpressionResult>(),
                References = references
            };
        }

        /// <summary>
        /// 按当前 Main / Sub 激活芯片匹配并追加组合技结果。
        /// 组合技在表达层特殊成立，但一旦形成结果，后续仍按普通单结果消费。
        /// </summary>
        private static List<FormalExpressionResult> BuildComboResults(
            Pawn pawn,
            ITriggerLoadoutReader triggerLoadoutReader,
            ExpressionSourceMaterial mainMaterial,
            ExpressionSourceMaterial subMaterial,
            FormalExpressionResult mainPrimary,
            FormalExpressionResult subPrimary,
            List<CompositeExpressionReference> references)
        {
            List<FormalExpressionResult> target = new List<FormalExpressionResult>();
            if (triggerLoadoutReader == null)
            {
                BdpDiagnostics.Throttled(
                    "expression.combo.resolve.reader_null",
                    "combo表达未成立：triggerLoadoutReader 为空。",
                    60);
                return target;
            }

            ITriggerSlotState mainSlot = triggerLoadoutReader.GetActiveSlot(TriggerSide.Main);
            ITriggerSlotState subSlot = triggerLoadoutReader.GetActiveSlot(TriggerSide.Sub);
            Thing mainChip = mainSlot != null ? mainSlot.LoadedChip : null;
            Thing subChip = subSlot != null ? subSlot.LoadedChip : null;
            LogComboProbe(
                "active_pair",
                mainChip,
                subChip,
                mainPrimary,
                subPrimary,
                "开始解析 combo。");
            if (mainChip?.def == null || subChip?.def == null)
            {
                LogComboProbe(
                    "missing_active_chip",
                    mainChip,
                    subChip,
                    mainPrimary,
                    subPrimary,
                    "combo表达未成立：Main/Sub 激活槽缺少芯片 Def。");
                return target;
            }

            string matchFailureReason;
            ComboDefinitionReadResult comboReadResult = ComboSurfaceAccess.FindMatch(
                mainChip,
                subChip,
                out matchFailureReason);
            if (comboReadResult == null
                || comboReadResult.Validation == null
                || !comboReadResult.Validation.IsValid
                || comboReadResult.Contract == null
                || comboReadResult.Contract.Expression == null
                || !comboReadResult.Contract.Expression.HasExpressionBlock
                || comboReadResult.Contract.Expression.Config == null
                || comboReadResult.Contract.Expression.Config.Entries == null
                || comboReadResult.Contract.Expression.Config.Entries.Count == 0)
            {
                LogComboProbe(
                    "find_or_validate_failed",
                    mainChip,
                    subChip,
                    mainPrimary,
                    subPrimary,
                    "combo表达未成立："
                    + DescribeComboReadResult(comboReadResult)
                    + " 匹配摘要="
                    + SafeText(matchFailureReason));
                return target;
            }

            ComboDefinitionContractResolver comboResolver = ComboSurfaceAccess.ResolveContractResolver();
            BDP.Core.Requirements.PawnRequirementCheckResult useRequirementCheck =
                ComboUseRequirementService.Instance.Evaluate(pawn, comboReadResult.ComboDef);
            List<ComboExpressionEntryConfig> comboEntries = ComboExpressionEntryCloneService.CloneEntries(
                comboReadResult.Contract.Expression.Config.Entries);
            string commonSourceVariantKey = ResolveCommonSourceVariantKey(mainMaterial, subMaterial);
            string commonSourceVariantLabel = ResolveCommonSourceVariantLabel(
                mainMaterial,
                subMaterial,
                commonSourceVariantKey);
            ComboExpressionVariantModifierRegistry.Apply(comboEntries, commonSourceVariantKey);

            // 只解释已修正的组合条目副本，保证 Content 只修改组合结果显式声明的数据，
            // 不回写 ComboDef 缓存，也不重复处理第一、第二来源结果。
            ChipExpressionResolvedContract resolvedContract = comboEntryInterpreter.Resolve(
                null,
                new ChipExpressionConfig
                {
                    Entries = BuildComboInterpreterEntries(comboEntries)
                },
                triggerLoadoutReader);
            if (resolvedContract == null
                || resolvedContract.Validation == null
                || !resolvedContract.Validation.IsValid
                || resolvedContract.Contract == null
                || resolvedContract.Contract.Entries == null
                || resolvedContract.Contract.Entries.Count == 0)
            {
                LogComboProbe(
                    "entry_contract_invalid",
                    mainChip,
                    subChip,
                    mainPrimary,
                    subPrimary,
                    "combo表达未成立：组合条目解释失败。"
                    + DescribeResolvedContract(resolvedContract));
                return target;
            }

            for (int i = 0; i < resolvedContract.Contract.Entries.Count; i++)
            {
                ComboExpressionEntryConfig entryConfig = comboEntries[i];
                ChipExpressionEntryContract entry = resolvedContract.Contract.Entries[i];
                ComboResolvedVerbProps resolvedVerbProps = comboResolver.ResolveVerbProps(
                    entryConfig,
                    mainPrimary,
                    subPrimary);
                ComboResolvedExecution resolvedExecution = comboResolver.ResolveExecution(
                    entryConfig,
                    mainPrimary,
                    subPrimary);
                FormalExpressionResult comboResult = BuildComboResult(
                    comboReadResult,
                    entryConfig,
                    entry,
                    mainMaterial,
                    subMaterial,
                    mainPrimary,
                    subPrimary,
                    resolvedVerbProps,
                    resolvedExecution,
                    useRequirementCheck,
                    commonSourceVariantKey,
                    commonSourceVariantLabel);
                if (comboResult == null)
                {
                    continue;
                }

                target.Add(comboResult);
                if (references != null)
                {
                    references.Add(new CompositeExpressionReference
                    {
                        CompositeId = comboResult.Id,
                        CompositeKind = CompositeExpressionKind.Combo,
                        SourceResultIds = BuildComboSourceIds(mainPrimary, subPrimary),
                        MainSourceResultId = mainPrimary != null ? mainPrimary.Id : null,
                        SubSourceResultId = subPrimary != null ? subPrimary.Id : null
                    });
                }
            }

            LogComboProbe(
                "resolved_success",
                mainChip,
                subChip,
                mainPrimary,
                subPrimary,
                "combo表达成立：comboDef="
                + comboReadResult.ComboDef.defName
                + ", configEntries="
                + comboReadResult.Contract.Expression.Config.Entries.Count
                + ", resolvedEntries="
                + resolvedContract.Contract.Entries.Count
                + ", formalResults="
                + target.Count);
            return target;
        }

        /// <summary>
        /// 输出 combo 解析链关键断点诊断。
        /// 这里统一走节流日志，避免投影刷新时刷屏。
        /// </summary>
        private static void LogComboProbe(
            string stage,
            Thing mainChip,
            Thing subChip,
            FormalExpressionResult mainPrimary,
            FormalExpressionResult subPrimary,
            string message)
        {
            string key = "expression.combo."
                + stage + "."
                + (mainChip?.def?.defName ?? "null") + "."
                + (subChip?.def?.defName ?? "null");
            BdpDiagnostics.Throttled(
                key,
                "combo诊断"
                + " | stage=" + stage
                + " | mainChip=" + DescribeChip(mainChip)
                + " | subChip=" + DescribeChip(subChip)
                + " | mainPrimary=" + DescribeResult(mainPrimary)
                + " | subPrimary=" + DescribeResult(subPrimary)
                + " | " + message,
                60);
        }

        /// <summary>
        /// 输出 combo 读取结果摘要。
        /// </summary>
        private static string DescribeComboReadResult(ComboDefinitionReadResult readResult)
        {
            if (readResult == null)
            {
                return "FindMatch 返回 null。";
            }

            return "comboDef=" + SafeText(readResult.ComboDef?.defName)
                + ", validation=" + DescribeValidation(readResult.Validation)
                + ", hasContract=" + (readResult.Contract != null)
                + ", hasExpression=" + (readResult.Contract?.Expression != null)
                + ", hasExpressionBlock=" + (readResult.Contract?.Expression != null && readResult.Contract.Expression.HasExpressionBlock)
                + ", entryCount=" + (readResult.Contract?.Expression?.Config?.Entries != null
                    ? readResult.Contract.Expression.Config.Entries.Count
                    : 0)
                + ".";
        }

        /// <summary>
        /// 输出条目解释结果摘要。
        /// </summary>
        private static string DescribeResolvedContract(ChipExpressionResolvedContract resolvedContract)
        {
            if (resolvedContract == null)
            {
                return "resolvedContract=null。";
            }

            return "validation=" + (resolvedContract.Validation != null
                    ? ("isValid=" + resolvedContract.Validation.IsValid
                        + ", errors=" + resolvedContract.Validation.Errors.Count
                        + ", warnings=" + resolvedContract.Validation.Warnings.Count
                        + ", firstError=" + SafeText(resolvedContract.Validation.Errors.Count > 0 ? resolvedContract.Validation.Errors[0] : null))
                    : "null")
                + ", entryCount=" + (resolvedContract.Contract?.Entries != null
                    ? resolvedContract.Contract.Entries.Count
                    : 0)
                + ".";
        }

        /// <summary>
        /// 输出 combo 校验结果摘要。
        /// </summary>
        private static string DescribeValidation(ComboDefinitionValidationResult validation)
        {
            if (validation == null)
            {
                return "null";
            }

            string firstError = validation.Errors != null && validation.Errors.Count > 0
                ? validation.Errors[0].Code + ":" + validation.Errors[0].Message
                : null;
            return "isValid=" + validation.IsValid
                + ", errors=" + (validation.Errors != null ? validation.Errors.Count : 0)
                + ", warnings=" + (validation.Warnings != null ? validation.Warnings.Count : 0)
                + ", firstError=" + SafeText(firstError);
        }

        /// <summary>
        /// 输出芯片摘要。
        /// </summary>
        private static string DescribeChip(Thing chip)
        {
            if (chip == null)
            {
                return "null";
            }

            return SafeText(chip.def?.defName)
                + "(" + SafeText(chip.ThingID) + ")";
        }

        /// <summary>
        /// 输出正式结果摘要。
        /// </summary>
        private static string DescribeResult(FormalExpressionResult result)
        {
            if (result == null)
            {
                return "null";
            }

            return SafeText(result.Id)
                + "["
                + result.CompositeKind
                + "/"
                + result.WeaponMode
                + "/"
                + result.VerbAttackRole
                + "]";
        }

        /// <summary>
        /// 把空字符串安全压缩成日志文本。
        /// </summary>
        private static string SafeText(string value)
        {
            return !string.IsNullOrWhiteSpace(value) ? value : "null";
        }

        /// <summary>
        /// 把组合技作者条目映射成现有芯片表达解释器可直接消费的普通条目集合。
        /// 这里只做结构对齐，不在这里偷偷执行组合技字段求值。
        /// </summary>
        private static List<ChipExpressionEntryConfig> BuildComboInterpreterEntries(
            IReadOnlyList<ComboExpressionEntryConfig> entries)
        {
            List<ChipExpressionEntryConfig> mapped = new List<ChipExpressionEntryConfig>();
            if (entries == null)
            {
                return mapped;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                ComboExpressionEntryConfig entry = entries[i];
                if (entry == null)
                {
                    continue;
                }

                mapped.Add(entry.ToChipExpressionEntryConfig());
            }

            return mapped;
        }

        /// <summary>
        /// 解析第一、第二来源项共同使用的来源变体键。
        /// 空白键统一视为没有来源变体；不一致时不构造组合级来源键。
        /// </summary>
        private static string ResolveCommonSourceVariantKey(
            ExpressionSourceMaterial firstSourceMaterial,
            ExpressionSourceMaterial secondSourceMaterial)
        {
            string firstSourceVariantKey = NormalizeSourceVariantKey(firstSourceMaterial?.SourceVariantKey);
            string secondSourceVariantKey = NormalizeSourceVariantKey(secondSourceMaterial?.SourceVariantKey);
            if (firstSourceVariantKey == null || secondSourceVariantKey == null)
            {
                return null;
            }

            return string.Equals(
                    firstSourceVariantKey,
                    secondSourceVariantKey,
                    System.StringComparison.OrdinalIgnoreCase)
                ? firstSourceVariantKey
                : null;
        }

        /// <summary>解析共同来源构型的显示标签。</summary>
        private static string ResolveCommonSourceVariantLabel(
            ExpressionSourceMaterial firstSourceMaterial,
            ExpressionSourceMaterial secondSourceMaterial,
            string commonSourceVariantKey)
        {
            if (string.IsNullOrWhiteSpace(commonSourceVariantKey))
            {
                return null;
            }

            return !string.IsNullOrWhiteSpace(firstSourceMaterial?.SourceVariantLabel)
                ? firstSourceMaterial.SourceVariantLabel
                : secondSourceMaterial?.SourceVariantLabel;
        }

        /// <summary>把来源变体键归一化为稳定比较值。</summary>
        private static string NormalizeSourceVariantKey(string sourceVariantKey)
        {
            return string.IsNullOrWhiteSpace(sourceVariantKey)
                ? null
                : sourceVariantKey.Trim();
        }

        /// <summary>
        /// 把组合技解释后的表达条目翻译成正式结果。
        /// 这一层只负责表达总表成立，不在这里新开组合技专用执行语义。
        /// </summary>
        private static FormalExpressionResult BuildComboResult(
            ComboDefinitionReadResult comboReadResult,
            ComboExpressionEntryConfig entryConfig,
            ChipExpressionEntryContract entry,
            ExpressionSourceMaterial mainMaterial,
            ExpressionSourceMaterial subMaterial,
            FormalExpressionResult mainPrimary,
            FormalExpressionResult subPrimary,
            ComboResolvedVerbProps resolvedVerbProps,
            ComboResolvedExecution resolvedExecution,
            BDP.Core.Requirements.PawnRequirementCheckResult useRequirementCheck,
            string sourceVariantKey,
            string sourceVariantLabel)
        {
            return comboResultFactory.Build(new ComboFormalExpressionResolution
            {
                ComboReadResult = comboReadResult,
                EntryConfig = entryConfig,
                EntryContract = entry,
                MainSourceMaterial = mainMaterial,
                SubSourceMaterial = subMaterial,
                MainSourceResult = mainPrimary,
                SubSourceResult = subPrimary,
                ResolvedVerbProps = resolvedVerbProps,
                ResolvedExecution = resolvedExecution,
                UseRequirementCheck = useRequirementCheck,
                SourceVariantKey = sourceVariantKey,
                SourceVariantLabel = sourceVariantLabel
            });
        }

        /// <summary>
        /// 按单侧结果集合回到该侧第一条内部来源材料。
        /// 这里只恢复“这侧芯片上下文是什么”，不把它等同成某一条主 Verb 结果。
        /// </summary>
        private static ExpressionSourceMaterial ResolveSourceMaterial(
            IReadOnlyList<ExpressionSourceMaterial> sourceMaterials,
            TriggerSide side)
        {
            if (sourceMaterials == null)
            {
                return null;
            }

            for (int i = 0; i < sourceMaterials.Count; i++)
            {
                ExpressionSourceMaterial material = sourceMaterials[i];
                if (material == null || material.Side != side)
                {
                    continue;
                }

                /*
                  Combo 的来源材料不能依赖“该侧已有正式发布结果”。
                  像旋空这类只提供 combo 来源、不单独发布业务的 passive 来源条目，
                  仍然必须把副侧 Trion 来源保留下来，供 FollowSecondSource 正式求值。
                */
                return material;
            }

            return null;
        }

        /// <summary>
        /// 按正式结果标识精确查找对应的来源材料。
        /// FormalExpressionResult.Id 与 ExpressionSourceMaterial.Id 一致，
        /// 因此可以通过 materialIndex 或线性扫描精确定位。
        /// 优先使用 materialIndex（O(1)），回退到线性扫描。
        /// </summary>
        private static ExpressionSourceMaterial ResolveMaterialForResult(
            IReadOnlyList<ExpressionSourceMaterial> sourceMaterials,
            IReadOnlyDictionary<string, ExpressionSourceMaterial> materialIndex,
            FormalExpressionResult result)
        {
            if (result == null || string.IsNullOrWhiteSpace(result.Id))
            {
                return null;
            }

            if (materialIndex != null)
            {
                ExpressionSourceMaterial material;
                if (materialIndex.TryGetValue(result.Id, out material))
                {
                    return material;
                }
            }

            if (sourceMaterials != null)
            {
                for (int i = 0; i < sourceMaterials.Count; i++)
                {
                    if (sourceMaterials[i]?.Id == result.Id)
                    {
                        return sourceMaterials[i];
                    }
                }
            }

            return null;
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
        /// 当前组合技在攻击层仍表现为普通单结果，不另外新开宿主槽体系。
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
                SourceKind = entry != null ? entry.SemanticSourceKind : SemanticSourceKind.Unknown,
                ReasonKey = comboDef != null ? comboDef.defName : null
            };
        }

        /// <summary>
        /// 把组合技条目暴露数据翻译成正式结果层数据。
        /// 这里沿用芯片表达同样的最小翻译规则，不另加组合技专用语义。
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
        /// 解析组合技结果应采用的正式运行时 Verb 规格。
        /// 显式条目优先，缺失字段才用组合技求值规则补齐。
        /// </summary>
        private static ResolvedVerbSpec ResolveComboVerbSpec(
            VerbProperties fallbackVerbProps,
            ResolvedVerbSpec fallbackVerbSpec,
            ComboResolvedVerbProps resolvedVerbProps)
        {
            ResolvedVerbSpec baseSpec = fallbackVerbSpec
                ?? ResolvedVerbSpecFactory.FromDeclared(
                    fallbackVerbProps,
                    null,
                    new List<Tool>(),
                    new List<MeleeToolSurface>(),
                    null);
            return ResolvedVerbSpecFactory.ApplyComboOverrides(baseSpec, resolvedVerbProps);
        }

        /// <summary>
        /// 解析组合技结果应采用的执行风格。
        /// 显式条目优先，其次回退到当前来源结果的已成立风格。
        /// </summary>
        private static AttackExecutionStyle ResolveComboExecutionStyle(
            ChipExpressionEntryContract entry,
            FormalExpressionResult mainPrimary,
            FormalExpressionResult subPrimary,
            ComboResolvedExecution resolvedExecution)
        {
            if (entry != null && entry.ExecutionStyle != null)
            {
                return entry.ExecutionStyle.Clone();
            }

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

            if (mainPrimary != null && mainPrimary.ExecutionStyle != null)
            {
                AttackExecutionStyle style = mainPrimary.ExecutionStyle.Clone();
                ApplyResolvedMeleeExecution(style, resolvedExecution);
                return style;
            }

            AttackExecutionStyle fallbackStyle = subPrimary != null ? subPrimary.ExecutionStyle?.Clone() : null;
            ApplyResolvedMeleeExecution(fallbackStyle, resolvedExecution);
            return fallbackStyle;
        }

        /// <summary>
        /// 把组合技求值后的近战节奏补到现有执行风格上。
        /// 只有近战字段才在这里被覆盖，远程节奏仍以条目显式声明为准。
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
        /// 生成组合技结果引用的来源结果标识列表。
        /// </summary>
        private static IReadOnlyList<string> BuildComboSourceIds(
            FormalExpressionResult mainPrimary,
            FormalExpressionResult subPrimary)
        {
            List<string> result = new List<string>();
            if (!string.IsNullOrWhiteSpace(mainPrimary?.Id))
            {
                result.Add(mainPrimary.Id);
            }

            if (!string.IsNullOrWhiteSpace(subPrimary?.Id))
            {
                result.Add(subPrimary.Id);
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
        /// 读取指定单侧集合中当前可作为主侧复合输入的 Primary 结果。
        /// 这里只选单侧主攻，不把双侧结果反向混入上游。
        /// </summary>
        private static FormalExpressionResult FindSingleSidePrimary(SingleSideExpressionSet set)
        {
            if (set?.WeaponResults == null)
            {
                return null;
            }

            for (int i = 0; i < set.WeaponResults.Count; i++)
            {
                FormalExpressionResult result = set.WeaponResults[i];
                if (result != null
                    && result.CompositeKind == CompositeExpressionKind.None
                    && result.VerbAttackRole == VerbAttackRole.Primary
                    && result.IsAvailable)
                {
                    return result;
                }
            }

            return null;
        }

        /// <summary>
        /// 尝试按当前双侧规则生成统一 DualPrimary 结果。
        /// 仅两侧为相同芯片且同为远程/同为近战时成立。
        /// 同芯片双持等价于单芯片子弹翻倍，无需异构合并。
        /// </summary>
        private static FormalExpressionResult BuildDualPrimaryResult(
            FormalExpressionResult mainPrimary,
            FormalExpressionResult subPrimary,
            ExpressionSourceMaterial mainMaterial,
            ExpressionSourceMaterial subMaterial,
            List<CompositeExpressionReference> references)
        {
            if (!CanBuildDualPrimary(mainPrimary, subPrimary, mainMaterial, subMaterial))
            {
                return null;
            }

            AttackExecutionStyle executionStyle = BuildDualExecutionStyle(mainPrimary, subPrimary);
            string resultId = "dual:" + mainPrimary.Id + "+" + subPrimary.Id;
            FormalExpressionResult dualResult = new FormalExpressionResult
            {
                Id = resultId,
                ResultKind = ExpressionResultKind.Verb,
                WeaponMode = mainPrimary.WeaponMode,
                OriginKind = ExpressionOriginKind.Composite,
                SourceVariantKey = mainPrimary != null ? mainPrimary.SourceVariantKey : null,
                SourceVariantLabel = mainPrimary != null ? mainPrimary.SourceVariantLabel : null,
                CompositeKind = CompositeExpressionKind.DualWeapon,
                // 同芯片双持——标签和展示名无需合并，取主侧即可。
                DisplayLabel = BuildDualDisplayLabel(mainPrimary),
                VisualPresetDefName = null,
                VisualGraphicOverrideDefName = mainPrimary != null ? mainPrimary.VisualGraphicOverrideDefName : null,
                CompositeVisualPresetDefName = mainPrimary != null ? mainPrimary.CompositeVisualPresetDefName : null,
                ForceSuppressHostEquipment = mainPrimary != null && mainPrimary.ForceSuppressHostEquipment,
                VisualPriority = mainPrimary != null ? mainPrimary.VisualPriority : 0,
                ManualEntryAggregationKey = BuildDualAggregationKey(mainPrimary, subPrimary),
                RoleKey = mainPrimary.RoleKey,
                VerbAttackRole = VerbAttackRole.Primary,
                Tags = mainPrimary?.Tags != null ? new List<string>(mainPrimary.Tags) : new List<string>(),
                ExecutionSlotKey = "DualPrimary",
                IsSecondaryAttack = false,
                Trion = BuildDualAggregateTrion(mainPrimary),
                IsAvailable = mainPrimary.IsAvailable && subPrimary.IsAvailable,
                CanProject = mainPrimary.CanProject && subPrimary.CanProject,
                SemanticContext = null,
                ModeKey = null,
                ExecutionStyle = executionStyle,
                VerbProps = mainPrimary.VerbProps,
                ResolvedVerbSpec = mainPrimary.ResolvedVerbSpec,
                Tool = null,
                DeclaredTools = new List<Tool>(),
                DeclaredMeleeToolSurfaces = new List<MeleeToolSurface>(),
                Maneuver = null,
                // 双侧模块分别克隆并标记来源，供暖机续建时的泳道重映射（CopyDualLanePrivateContexts）
                // 按 sourceResultId 精确对位。同芯片双持时两侧模块一致，拼接后数量翻倍；
                // 异构双持时各自保留自身模块，不会丢失副侧模块。
                RangedModules = BuildDualRangedModules(mainPrimary, subPrimary)
            };

            if (references != null)
            {
                references.Add(new CompositeExpressionReference
                {
                    CompositeId = resultId,
                    CompositeKind = CompositeExpressionKind.DualWeapon,
                    SourceResultIds = new List<string> { mainPrimary.Id, subPrimary.Id },
                    MainSourceResultId = mainPrimary.Id,
                    SubSourceResultId = subPrimary.Id
                });
            }

            return dualResult;
        }

        /// <summary>
        /// 构建双主聚合结果自己的 Trion 参数。
        /// 聚合结果保留使用费用和最低门槛，但不复制来源持续费用表，避免与两条来源结果重复扣费。
        /// </summary>
        private static ExpressionSourceTrionConfig BuildDualAggregateTrion(FormalExpressionResult mainPrimary)
        {
            if (mainPrimary?.Trion == null)
            {
                return null;
            }

            return new ExpressionSourceTrionConfig
            {
                UseCost = mainPrimary.Trion.UseCost,
                MinimumRequired = mainPrimary.Trion.MinimumRequired,
                SustainCostBySourceCount = new List<ExpressionSustainCostBySourceCountConfig>()
            };
        }



        /// <summary>
        /// 判断当前主副两侧是否允许生成双侧统一主攻结果。
        /// 必须同为 Verb、同为一种 WeaponMode，两侧是相同芯片且当前激活动作相同。
        /// </summary>
        private static bool CanBuildDualPrimary(
            FormalExpressionResult mainPrimary,
            FormalExpressionResult subPrimary,
            ExpressionSourceMaterial mainMaterial,
            ExpressionSourceMaterial subMaterial)
        {
            return mainPrimary != null
                && subPrimary != null
                && mainPrimary.ResultKind == ExpressionResultKind.Verb
                && subPrimary.ResultKind == ExpressionResultKind.Verb
                && mainPrimary.WeaponMode != WeaponExpressionMode.None
                && mainPrimary.WeaponMode == subPrimary.WeaponMode
                && IsSameChipIdentity(mainMaterial, subMaterial)
                && IsSameEntryAction(mainMaterial, subMaterial);
        }

        /// <summary>
        /// 判断两侧来源材料是否属于相同芯片。
        /// 芯片身份统一由 ThingDef 与中性来源变体键共同决定。
        /// </summary>
        private static bool IsSameChipIdentity(
            ExpressionSourceMaterial mainMaterial,
            ExpressionSourceMaterial subMaterial)
        {
            if (mainMaterial?.SourceChip?.def == null || subMaterial?.SourceChip?.def == null)
            {
                return false;
            }

            if (mainMaterial.SourceChip.def != subMaterial.SourceChip.def)
            {
                return false;
            }

            return mainMaterial.SourceVariantKey == subMaterial.SourceVariantKey;
        }

        /// <summary>
        /// 判断两侧当前激活动作是否相同。
        /// 比较表达条目的稳定标识（RuntimePayload.Entry.Id），而非形态序号（ModeKey）。
        /// 同一模板的同一条目在不同芯片上 ID 一致；不同模板或不同条目 ID 不同。
        /// 例如：小行星=小行星 → 可双持；小行星≠毒蛇 → 不可双持。
        /// </summary>
        private static bool IsSameEntryAction(
            ExpressionSourceMaterial mainMaterial,
            ExpressionSourceMaterial subMaterial)
        {
            string mainEntryId = mainMaterial?.RuntimePayload?.Entry?.Id;
            string subEntryId = subMaterial?.RuntimePayload?.Entry?.Id;
            return mainEntryId != null && mainEntryId == subEntryId;
        }

        /// <summary>
        /// 为双侧统一主攻结果构造正式执行风格。
        /// 双远按双方远程节奏映射，双近固定映射到 MainThenSub。
        /// </summary>
        private static AttackExecutionStyle BuildDualExecutionStyle(
            FormalExpressionResult mainPrimary,
            FormalExpressionResult subPrimary)
        {
            return new AttackExecutionStyle
            {
                Dual = new DualAttackExecutionStyle
                {
                    Schedule = ResolveDualExecutionSchedule(mainPrimary, subPrimary)
                }
            };
        }

        /// <summary>
        /// 解析当前双侧统一结果应使用的正式调度方式。
        /// 同芯片两侧节奏必然一致，仅按远程/近战和节奏定调度。
        /// </summary>
        private static DualExecutionSchedule ResolveDualExecutionSchedule(
            FormalExpressionResult mainPrimary,
            FormalExpressionResult subPrimary)
        {
            if (mainPrimary == null || subPrimary == null)
            {
                return DualExecutionSchedule.None;
            }

            if (mainPrimary.WeaponMode == WeaponExpressionMode.Melee)
            {
                return DualExecutionSchedule.MainThenSub;
            }

            RangedExecutionRhythm rhythm = mainPrimary.ExecutionStyle?.Single != null
                ? mainPrimary.ExecutionStyle.Single.RangedRhythm
                : RangedExecutionRhythm.None;

            if (rhythm == RangedExecutionRhythm.Sequential)
            {
                return DualExecutionSchedule.Alternating;
            }

            if (rhythm == RangedExecutionRhythm.Simultaneous)
            {
                return DualExecutionSchedule.Simultaneous;
            }

            return DualExecutionSchedule.Alternating;
        }

        /// <summary>
        /// 生成双侧统一主攻结果的最小显示名。
        /// 同芯片双持只需一份标签。
        /// </summary>
        private static string BuildDualDisplayLabel(FormalExpressionResult mainPrimary)
        {
            string mainLabel = !string.IsNullOrWhiteSpace(mainPrimary?.DisplayLabel)
                ? mainPrimary.DisplayLabel
                : "双持";
            return "双持·" + mainLabel;
        }

        /// <summary>
        /// 为双持入口构建稳定的手动聚合键。
        /// 当前不带 side，仅按双方主入口的稳定键组合，并做字典序归一化。
        /// </summary>
        private static string BuildDualAggregationKey(FormalExpressionResult mainPrimary, FormalExpressionResult subPrimary)
        {
            string firstKey = !string.IsNullOrWhiteSpace(mainPrimary?.ManualEntryAggregationKey)
                ? mainPrimary.ManualEntryAggregationKey
                : mainPrimary != null ? mainPrimary.Id : "null";
            string secondKey = !string.IsNullOrWhiteSpace(subPrimary?.ManualEntryAggregationKey)
                ? subPrimary.ManualEntryAggregationKey
                : subPrimary != null ? subPrimary.Id : "null";

            if (string.CompareOrdinal(firstKey, secondKey) > 0)
            {
                string temp = firstKey;
                firstKey = secondKey;
                secondKey = temp;
            }

            return "dual:" + firstKey + "+" + secondKey;
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
        /// 按来源结果标识克隆单侧模块挂载列表。
        /// 克隆后的每个挂载都绑定到指定来源结果，供运行时按来源隔离影响范围。
        /// </summary>
        private static List<RangedModuleMountConfig> CloneRangedModulesWithSource(
            IReadOnlyList<RangedModuleMountConfig> modules,
            string sourceResultId)
        {
            List<RangedModuleMountConfig> result = new List<RangedModuleMountConfig>();
            if (modules == null || string.IsNullOrWhiteSpace(sourceResultId))
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

                RangedModuleMountConfig clone = module.Clone();
                clone.sourceResultId = sourceResultId;
                result.Add(clone);
            }

            return result;
        }

        /// <summary>
        /// 构建双持复合结果应持有的完整模块挂载列表。
        /// 主侧和副侧各自克隆全量模块并标记来源标识，拼接后返回。
        /// 同芯片双持→ 模块数量翻倍（各侧一份）；异构双持→ 各自模块全保留。
        /// </summary>
        private static IReadOnlyList<RangedModuleMountConfig> BuildDualRangedModules(
            FormalExpressionResult mainPrimary,
            FormalExpressionResult subPrimary)
        {
            List<RangedModuleMountConfig> result = new List<RangedModuleMountConfig>();
            string mainSourceId = mainPrimary != null ? mainPrimary.Id : null;
            string subSourceId = subPrimary != null ? subPrimary.Id : null;

            if (mainPrimary?.RangedModules != null)
            {
                result.AddRange(CloneRangedModulesWithSource(mainPrimary.RangedModules, mainSourceId));
            }

            if (subPrimary?.RangedModules != null)
            {
                result.AddRange(CloneRangedModulesWithSource(subPrimary.RangedModules, subSourceId));
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
