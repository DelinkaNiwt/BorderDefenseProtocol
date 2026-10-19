using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using BDP.Content.Assembly.ChipManufacturing.Model;
using BDP.Content.Assembly.ChipManufacturing.Resolution;
using BDP.Content.Assembly.ChipManufacturing.Thing;
using BDP.Core.Expressions;
using BDP.Core.Information;
using BDP.Core.Information.Vanilla;
using BDP.Support.Diagnostics;
using RimWorld;
using Verse;

namespace BDP.Content.Information
{
    /// <summary>
    /// 把成品芯片的统一信息模型和正规化武器契约发布到原版信息卡。
    /// 普通字段与原版武器统计并列，不把 BDP 文本伪装成原版武器属性。
    /// </summary>
    public static class ManufacturedChipInfoCardPublisher
    {
        /// <summary>
        /// 原版信息卡允许发布的普通标量字段白名单。
        /// 复杂列表、上下文、来源链和武器字段继续由 BDP 详情界面承载。
        /// </summary>
        private static readonly HashSet<string> PublishedFieldKeys =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "description",
                "capacityCost",
                "activationCost",
                "activationDelay",
                "deactivationDelay",
                "UseCost",
                "MinimumRequired",
                "trionUseCost",
                "trionMinimumRequired",
                "effectRadius",
                "effectDuration",
                "abilityLabel",
                "abilityDescription"
            };

        /// <summary>芯片规格应当独立显示的字段键。</summary>
        private static readonly HashSet<string> ChipSpecificationFieldKeys =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "capacityCost",
                "activationCost",
                "activationDelay",
                "deactivationDelay",
                "UseCost",
                "MinimumRequired",
                "trionUseCost",
                "trionMinimumRequired"
            };

        /// <summary>芯片规格分类的原版统计排序位置。</summary>
        private const int ChipSpecificationCategoryOrder = 42;

        /// <summary>BDP 逐 Tool 近战分类的起始排序位置。</summary>
        private const int BdpMeleeToolCategoryOrder = 50;

        /// <summary>发布一枚成品芯片当前可见的原版信息条目。</summary>
        public static IEnumerable<StatDrawEntry> Publish(CompManufacturedChip manufactured)
        {
            List<StatDrawEntry> result = new List<StatDrawEntry>();
            if (manufactured == null)
            {
                return result;
            }

            try
            {
                ChipCombinationResolution resolution;
                if (!manufactured.TryGetCurrentResolution(out resolution))
                {
                    return result;
                }

                ChipInformationModel model =
                    ManufacturedChipInformationModelBuilder.Build(resolution);
                AddInformationEntries(model, result);

                ChipExpressionResolvedContract contract =
                    ChipExpressionContractSurfaceAccess.Resolve(manufactured.parent, null);
                AddVanillaWeaponEntries(contract, result);
            }
            catch (Exception exception)
            {
                BdpDiagnostics.Once(
                    "manufactured_chip_info_card.failure",
                    "成品芯片原版信息发布失败：" + exception.GetType().Name);
            }

            return result;
        }

        /// <summary>按白名单把统一模型中的普通字段转为自定义信息条目。</summary>
        private static void AddInformationEntries(
            ChipInformationModel model,
            ICollection<StatDrawEntry> target)
        {
            if (model?.Root?.Children == null || target == null)
            {
                return;
            }

            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            ChipSpecificationAccumulator specifications =
                new ChipSpecificationAccumulator();
            for (int index = 0; index < model.Root.Children.Count; index++)
            {
                ChipInformationNode node = model.Root.Children[index];
                if (node == null
                    || string.Equals(node.KindKey, "feature", StringComparison.Ordinal)
                    || string.Equals(node.KindKey, "context", StringComparison.Ordinal)
                    || string.Equals(node.KindKey, "relation", StringComparison.Ordinal))
                {
                    continue;
                }

                AddBlockInformationEntries(
                    node,
                    target,
                    seen,
                    null,
                    specifications);
            }

            AddChipSpecificationEntries(specifications, target);
        }

        /// <summary>递归读取模型块，但不展开上下文和来源链。</summary>
        private static void AddBlockInformationEntries(
            ChipInformationNode block,
            ICollection<StatDrawEntry> target,
            ISet<string> seen,
            string parentKey,
            ChipSpecificationAccumulator specifications)
        {
            if (block == null)
            {
                return;
            }

            string blockKey = string.IsNullOrWhiteSpace(parentKey)
                ? block.Key
                : parentKey + "." + block.Key;
            if (IsSustainCostEntry(block))
            {
                specifications?.AddSustainCostLine(block);
                return;
            }

            if (block.Properties != null)
            {
                for (int index = 0; index < block.Properties.Count; index++)
                {
                    ChipInformationProperty field = block.Properties[index];
                    string fieldIdentity = blockKey + "." + field?.Key;
                    if (!seen.Add(fieldIdentity))
                    {
                        continue;
                    }

                    if (!ShouldPublish(field)
                        || !TryFormatChipInformationProperty(field, out string value))
                    {
                        continue;
                    }

                    if (IsChipSpecificationField(field))
                    {
                        specifications?.AddField(field, block.LabelKey);
                        continue;
                    }

                    string label = field.LabelKey.NullOrEmpty()
                        ? field.Key
                        : field.LabelKey.Translate().ToString();
                    string reportText = block.LabelKey.NullOrEmpty()
                        ? "BDP_Info_Block_Descriptions".Translate().ToString()
                        : block.LabelKey.Translate().ToString();
                    target.Add(new StatDrawEntry(
                        StatCategoryDefOf.BasicsImportant,
                        label,
                        value,
                        BuildFieldReport(field, reportText, value),
                        8000 - block.Order));
                }
            }

            if (block.Children == null)
            {
                return;
            }

            for (int index = 0; index < block.Children.Count; index++)
            {
                AddBlockInformationEntries(
                    block.Children[index],
                    target,
                    seen,
                    blockKey,
                    specifications);
            }
        }

        /// <summary>把芯片规格字段发布为单独分类，持续消耗保持单条目多行值。</summary>
        private static void AddChipSpecificationEntries(
            ChipSpecificationAccumulator specifications,
            ICollection<StatDrawEntry> target)
        {
            if (specifications == null || target == null)
            {
                return;
            }

            StatCategoryDef category = CreateChipSpecificationsCategory();
            HashSet<string> emitted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int index = 0; index < specifications.Fields.Count; index++)
            {
                ChipSpecificationField field = specifications.Fields[index];
                if (field == null || !emitted.Add(field.Field.Key))
                {
                    continue;
                }

                if (!TryFormatChipSpecificationValue(field.Field, out string value))
                {
                    continue;
                }

                string label = field.Field.LabelKey.NullOrEmpty()
                    ? field.Field.Key
                    : field.Field.LabelKey.Translate().ToString();
                string reportText = field.ReportLabelKey.NullOrEmpty()
                    ? "BDP_Info_Block_ChipSpecifications".Translate().ToString()
                    : field.ReportLabelKey.Translate().ToString();
                target.Add(new StatDrawEntry(
                    category,
                    label,
                    value,
                    BuildFieldReport(field.Field, reportText, value),
                    8000 - index));
            }

            if (specifications.SustainCostLines.Count > 0)
            {
                target.Add(new StatDrawEntry(
                    category,
                    "BDP_Info_Field_SustainCost".Translate().ToString(),
                    FormatMultilineValue(specifications.SustainCostLines),
                    "BDP_Info_Block_ChipSpecifications".Translate().ToString(),
                    7900));
            }
        }

        /// <summary>创建芯片规格分类，不注册到原版 DefDatabase。</summary>
        private static StatCategoryDef CreateChipSpecificationsCategory()
        {
            return new StatCategoryDef
            {
                defName = "BDP_ChipSpecifications",
                label = "BDP_Info_Category_ChipSpecifications".Translate().ToString(),
                displayOrder = ChipSpecificationCategoryOrder,
                displayAllByDefault = true
            };
        }

        /// <summary>判断一个字段是否属于芯片规格。</summary>
        private static bool IsChipSpecificationField(ChipInformationProperty field)
        {
            return field != null && ChipSpecificationFieldKeys.Contains(field.Key);
        }

        /// <summary>判断当前块是否是一条按来源数量记录的持续消耗。</summary>
        private static bool IsSustainCostEntry(ChipInformationNode block)
        {
            return block != null
                && string.Equals(
                    block.KindKey,
                    "sustainCostBySourceCountEntry",
                    StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>格式化普通芯片信息字段，并为 Trion 数值补充单位。</summary>
        private static bool TryFormatChipInformationProperty(
            ChipInformationProperty field,
            out string text)
        {
            return TryFormat(field, out text);
        }

        /// <summary>把芯片规格数值格式化为最终值。</summary>
        private static bool TryFormatChipSpecificationValue(
            ChipInformationProperty field,
            out string text)
        {
            text = string.Empty;
            if (field == null || field.Value == null)
            {
                return false;
            }

            if (!TryFormat(field, out string scalar))
            {
                if (field.ValueKind != ChipInformationValueKind.Number)
                {
                    return false;
                }

                scalar = field.Value.Number.ToString("0.##", CultureInfo.InvariantCulture);
            }

            if (string.Equals(field.Key, "UseCost", StringComparison.OrdinalIgnoreCase)
                || string.Equals(field.Key, "MinimumRequired", StringComparison.OrdinalIgnoreCase)
                || string.Equals(field.Key, "trionUseCost", StringComparison.OrdinalIgnoreCase)
                || string.Equals(field.Key, "trionMinimumRequired", StringComparison.OrdinalIgnoreCase))
            {
                text = "BDP_Info_Unit_Trion".Translate(scalar).ToString();
            }
            else
            {
                text = scalar;
            }

            return true;
        }

        /// <summary>把多来源持续消耗值拼成一个多行值。</summary>
        private static string FormatMultilineValue(IList<string> lines)
        {
            StringBuilder builder = new StringBuilder();
            if (lines == null)
            {
                return string.Empty;
            }

            for (int index = 0; index < lines.Count; index++)
            {
                if (string.IsNullOrWhiteSpace(lines[index]))
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append(Environment.NewLine);
                }

                builder.Append(lines[index]);
            }

            return builder.ToString();
        }

        /// <summary>判断普通字段是否在原版信息卡白名单中且有可用值。</summary>
        private static bool ShouldPublish(ChipInformationProperty field)
        {
            return field != null
                && PublishedFieldKeys.Contains(field.Key)
                && field.Value != null
                && field.ValueKind != ChipInformationValueKind.Enum
                && field.ValueKind != ChipInformationValueKind.List;
        }

        /// <summary>格式化白名单中的标量值，并套用字段单位语言键。</summary>
        private static bool TryFormat(
            ChipInformationProperty field,
            out string text)
        {
            text = string.Empty;
            if (field == null || field.Value == null)
            {
                return false;
            }

            return TryFormatValue(field.UnitKey, field.Value, field.ValueKind, out text);
        }

        /// <summary>统一格式化普通字段值和来源贡献值。</summary>
        private static bool TryFormatValue(
            string unitKey,
            ChipInformationValue value,
            ChipInformationValueKind valueKind,
            out string text)
        {
            text = string.Empty;
            if (value == null)
            {
                return false;
            }

            if (valueKind == ChipInformationValueKind.Enum
                || valueKind == ChipInformationValueKind.List)
            {
                return false;
            }

            text = ChipInformationTextFormatter.FormatValueWithUnit(
                value,
                valueKind,
                unitKey,
                CultureInfo.InvariantCulture);
            return true;
        }

        /// <summary>
        /// 构建原版信息条目的右侧详情。
        /// 无来源链时保留原有块标题；有来源链时追加最终值和贡献明细。
        /// </summary>
        private static string BuildFieldReport(
            ChipInformationProperty field,
            string reportText,
            string finalValue)
        {
            StringBuilder builder = new StringBuilder(reportText ?? string.Empty);
            if (!string.IsNullOrWhiteSpace(finalValue))
            {
                AppendReportLine(
                    builder,
                    "BDP_Info_Report_FieldFinalValue".Translate(finalValue).ToString());
            }

            if (field?.Sources == null || field.Sources.Count == 0)
            {
                return builder.ToString();
            }

            AppendReportLine(
                builder,
                "BDP_Info_Report_FieldProvenance".Translate().ToString());
            for (int index = 0; index < field.Sources.Count; index++)
            {
                ChipInformationProvenance provenance = field.Sources[index];
                if (provenance == null || !provenance.Applied)
                {
                    continue;
                }

                if (!TryFormatProvenanceValue(field, provenance.Value, out string value))
                {
                    continue;
                }

                string contribution = TranslateContributionKind(
                    provenance.ContributionKind);
                string source = "BDP_Info_Report_Source".Translate(
                    TranslateSourceKind(provenance.SourceKind),
                    provenance.SourceKey ?? string.Empty).ToString();
                AppendReportLine(
                    builder,
                    "BDP_Info_Report_ContributionLine".Translate(
                        contribution,
                        value,
                        source).ToString());
            }

            return builder.ToString();
        }

        /// <summary>按字段单位格式化来源贡献值。</summary>
        private static bool TryFormatProvenanceValue(
            ChipInformationProperty field,
            ChipInformationValue value,
            out string text)
        {
            return TryFormatValue(
                field?.UnitKey,
                value,
                value?.Kind ?? ChipInformationValueKind.Text,
                out text);
        }

        /// <summary>追加一行右侧详情文本。</summary>
        private static void AppendReportLine(StringBuilder builder, string line)
        {
            if (builder == null || string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            if (builder.Length > 0)
            {
                builder.Append(Environment.NewLine);
            }

            builder.Append(line);
        }

        /// <summary>把贡献类型转换为语言键。</summary>
        private static string TranslateContributionKind(
            ChipInformationContributionKind kind)
        {
            switch (kind)
            {
                case ChipInformationContributionKind.Base:
                    return "BDP_Info_Report_Contribution_Base".Translate().ToString();
                case ChipInformationContributionKind.Variant:
                    return "BDP_Info_Report_Contribution_Variant".Translate().ToString();
                case ChipInformationContributionKind.Runtime:
                    return "BDP_Info_Report_Contribution_Runtime".Translate().ToString();
                case ChipInformationContributionKind.Replacement:
                    return "BDP_Info_Report_Contribution_Replacement".Translate().ToString();
                case ChipInformationContributionKind.Addition:
                    return "BDP_Info_Report_Contribution_Addition".Translate().ToString();
                case ChipInformationContributionKind.Multiplier:
                    return "BDP_Info_Report_Contribution_Multiplier".Translate().ToString();
                case ChipInformationContributionKind.Derived:
                    return "BDP_Info_Report_Contribution_Derived".Translate().ToString();
                case ChipInformationContributionKind.Suppressed:
                    return "BDP_Info_Report_Contribution_Suppressed".Translate().ToString();
                default:
                    return "BDP_Info_Report_Contribution_Unknown".Translate().ToString();
            }
        }

        /// <summary>把来源类别转换为面向玩家的简短标签。</summary>
        private static string TranslateSourceKind(ChipInformationSourceKind kind)
        {
            switch (kind)
            {
                case ChipInformationSourceKind.BaseDefinition:
                case ChipInformationSourceKind.DeclaredDefinition:
                case ChipInformationSourceKind.EffectiveDefinition:
                    return "BDP_Info_Report_Source_BaseDefinition".Translate().ToString();
                case ChipInformationSourceKind.Variant:
                    return "BDP_Info_Report_Source_Variant".Translate().ToString();
                case ChipInformationSourceKind.RuntimeResult:
                    return "BDP_Info_Report_Source_Runtime".Translate().ToString();
                default:
                    return "BDP_Info_Report_Source_Other".Translate().ToString();
            }
        }

        /// <summary>芯片规格发布阶段的字段和持续消耗临时集合。</summary>
        private sealed class ChipSpecificationAccumulator
        {
            /// <summary>待发布的单值规格字段。</summary>
            internal readonly List<ChipSpecificationField> Fields =
                new List<ChipSpecificationField>();

            /// <summary>待发布的持续消耗多行值。</summary>
            internal readonly List<string> SustainCostLines =
                new List<string>();

            /// <summary>加入一个芯片规格字段。</summary>
            internal void AddField(ChipInformationProperty field, string reportLabelKey)
            {
                if (field != null)
                {
                    Fields.Add(new ChipSpecificationField(field, reportLabelKey));
                }
            }

            /// <summary>读取一个持续消耗档位并加入多行值集合。</summary>
            internal void AddSustainCostLine(ChipInformationNode block)
            {
                if (block?.Properties == null)
                {
                    return;
                }

                ChipInformationProperty sourceCount = null;
                ChipInformationProperty totalPerSecond = null;
                for (int index = 0; index < block.Properties.Count; index++)
                {
                    ChipInformationProperty field = block.Properties[index];
                    if (field == null)
                    {
                        continue;
                    }

                    if (string.Equals(field.Key, "SourceCount", StringComparison.OrdinalIgnoreCase))
                    {
                        sourceCount = field;
                    }
                    else if (string.Equals(field.Key, "TotalPerSecond", StringComparison.OrdinalIgnoreCase))
                    {
                        totalPerSecond = field;
                    }
                }

                if (sourceCount == null
                    || totalPerSecond == null
                    || sourceCount.Value == null
                    || totalPerSecond.Value == null)
                {
                    return;
                }

                string sourceText = sourceCount.Value.Integer.ToString(CultureInfo.InvariantCulture);
                string totalText;
                if (!TryFormat(totalPerSecond, out totalText))
                {
                    totalText = totalPerSecond.Value.Number.ToString(
                        "0.##",
                        CultureInfo.InvariantCulture);
                }

                string value = "BDP_Info_Unit_TrionPerSecond".Translate(totalText).ToString();
                SustainCostLines.Add(
                    "BDP_Info_Field_SustainCostLine".Translate(sourceText, value).ToString());
            }
        }

        /// <summary>芯片规格字段及其原始详情块标题。</summary>
        private sealed class ChipSpecificationField
        {
            /// <summary>字段最终值。</summary>
            internal readonly ChipInformationProperty Field;

            /// <summary>字段详情所使用的块标题语言键。</summary>
            internal readonly string ReportLabelKey;

            /// <summary>创建芯片规格字段记录。</summary>
            internal ChipSpecificationField(
                ChipInformationProperty field,
                string reportLabelKey)
            {
                Field = field;
                ReportLabelKey = reportLabelKey;
            }
        }

        /// <summary>按正规化能力把原版生成的武器统计条目原样追加。</summary>
        private static void AddVanillaWeaponEntries(
            ChipExpressionResolvedContract contract,
            ICollection<StatDrawEntry> target)
        {
            if (contract?.Validation == null
                || !contract.Validation.IsValid
                || contract.Contract?.Entries == null)
            {
                return;
            }

            for (int index = 0; index < contract.Contract.Entries.Count; index++)
            {
                ChipExpressionEntryContract entry = contract.Contract.Entries[index];
                if (entry == null
                    || (!ChipExpressionCapabilityResolver.HasExecutableRangedSurface(entry)
                        && !ChipExpressionCapabilityResolver.HasExecutableMeleeSurface(entry)))
                {
                    continue;
                }

                VanillaWeaponStatSurface surface;
                string failureReason;
                if (!VanillaWeaponStatSurfaceFactory.TryCreate(
                    entry,
                    out surface,
                    out failureReason))
                {
                    continue;
                }

                IReadOnlyList<StatDrawEntry> entries;
                if (VanillaWeaponStatSurfaceReader.TryRead(
                    surface,
                    out entries,
                    out failureReason)
                    && entries != null)
                {
                    for (int statIndex = 0; statIndex < entries.Count; statIndex++)
                    {
                        if (entries[statIndex] != null)
                        {
                            target.Add(entries[statIndex]);
                        }
                    }
                }

                if (ChipExpressionCapabilityResolver.HasExecutableMeleeSurface(entry))
                {
                    AddPerToolMeleeEntries(entry, target, index);
                }
            }
        }

        /// <summary>按每把近战 Tool 发布独立的 BDP 统计块。</summary>
        private static void AddPerToolMeleeEntries(
            ChipExpressionEntryContract entry,
            ICollection<StatDrawEntry> target,
            int entryIndex)
        {
            if (entry?.DeclaredMeleeToolSurfaces == null || target == null)
            {
                return;
            }

            for (int index = 0; index < entry.DeclaredMeleeToolSurfaces.Count; index++)
            {
                BDP.Core.AttackExecution.MeleeToolSurface toolSurface =
                    entry.DeclaredMeleeToolSurfaces[index];
                VanillaWeaponStatSurface surface;
                string failureReason;
                if (!VanillaWeaponStatSurfaceFactory.TryCreateMeleeTool(
                    entry,
                    toolSurface,
                    out surface,
                    out failureReason))
                {
                    continue;
                }

                VanillaMeleeStatProjection projection;
                if (!VanillaMeleeStatProjection.TryCreate(surface, out projection))
                {
                    continue;
                }

                StatCategoryDef category = CreateMeleeToolCategory(
                    toolSurface.Tool.LabelCap,
                    entryIndex * 100 + index);
                string reportTitle = "BDP_Info_Report_MeleeTool".Translate(
                    toolSurface.Tool.LabelCap).ToString();
                target.Add(new StatDrawEntry(
                    category,
                    VanillaMeleeStatProjection.DamageLabel,
                    projection.DamagePerSecond.ToString("0.##", CultureInfo.InvariantCulture),
                    BuildMeleeDamageReport(projection),
                    8000));
                target.Add(new StatDrawEntry(
                    category,
                    VanillaMeleeStatProjection.ArmorPenetrationLabel,
                    projection.ArmorPenetration.ToStringPercent(),
                    BuildMeleeArmorPenetrationReport(projection),
                    7990,
                    reportTitle));
            }
        }

        /// <summary>创建逐 Tool 近战分类。</summary>
        private static StatCategoryDef CreateMeleeToolCategory(
            string toolLabel,
            int toolIndex)
        {
            return new StatCategoryDef
            {
                defName = "BDP_MeleeTool_" + toolIndex,
                label = "BDP_Info_Category_MeleeTool".Translate(toolLabel).ToString(),
                displayOrder = BdpMeleeToolCategoryOrder + toolIndex,
                displayAllByDefault = true
            };
        }

        /// <summary>创建逐 Tool 近战伤害详情。</summary>
        private static string BuildMeleeDamageReport(VanillaMeleeStatProjection projection)
        {
            return "BDP_Info_Report_MeleeToolDamage".Translate(
                projection.Damage.ToString("0.##", CultureInfo.InvariantCulture),
                projection.HitCount,
                projection.HitIntervalTicks,
                projection.CooldownSeconds.ToString("0.##", CultureInfo.InvariantCulture),
                projection.DamagePerSecond.ToString("0.##", CultureInfo.InvariantCulture)).ToString();
        }

        /// <summary>创建逐 Tool 近战护甲穿透详情。</summary>
        private static string BuildMeleeArmorPenetrationReport(
            VanillaMeleeStatProjection projection)
        {
            return "BDP_Info_Report_MeleeToolArmorPenetration".Translate(
                projection.ArmorPenetration.ToStringPercent()).ToString();
        }
    }
}
