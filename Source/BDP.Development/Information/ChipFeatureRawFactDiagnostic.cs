using System.Collections.Generic;
using System.Globalization;
using BDP.Core.Information;

namespace BDP.Development.Information
{
    /// <summary>Development 专用统一节点事实摘要。</summary>
    public static class ChipFeatureRawFactDiagnostic
    {
        /// <summary>捕获根节点及其子树中的类型化属性和原始来源。</summary>
        public static ChipFeatureRawFactReport Capture(ChipInformationModel model)
        {
            ChipFeatureRawFactReport report = new ChipFeatureRawFactReport();
            AppendNodes(report, model?.Root?.Children, null);
            return report;
        }

        /// <summary>递归写入节点身份、属性值和来源记录。</summary>
        private static void AppendNodes(
            ChipFeatureRawFactReport report,
            IReadOnlyList<ChipInformationNode> nodes,
            string contextKey)
        {
            if (report == null || nodes == null)
            {
                return;
            }

            for (int index = 0; index < nodes.Count; index++)
            {
                ChipInformationNode node = nodes[index];
                if (node == null)
                {
                    continue;
                }

                string nextContextKey = string.Equals(
                    node.KindKey,
                    "context",
                    System.StringComparison.Ordinal)
                    ? node.Key
                    : contextKey;
                if (string.Equals(node.KindKey, "feature", System.StringComparison.Ordinal)
                    || string.Equals(node.KindKey, "feature_group", System.StringComparison.Ordinal)
                    || string.Equals(node.KindKey, "feature_subgroup", System.StringComparison.Ordinal))
                {
                    report.Lines.Add(
                        (nextContextKey ?? "root") + " | " + node.Key
                        + " | kind=" + node.KindKey
                        + " | def=" + (node.DefinitionKey ?? "<none>"));
                    AppendProperties(report, nextContextKey ?? "root", node);
                }

                AppendNodes(report, node.Children, nextContextKey);
            }
        }

        /// <summary>写入节点属性的最终值与全部来源值。</summary>
        private static void AppendProperties(
            ChipFeatureRawFactReport report,
            string contextKey,
            ChipInformationNode node)
        {
            if (report == null || node?.Properties == null)
            {
                return;
            }

            for (int index = 0; index < node.Properties.Count; index++)
            {
                ChipInformationProperty property = node.Properties[index];
                if (property == null)
                {
                    continue;
                }

                report.Lines.Add(
                    "  " + contextKey + " | " + node.Key + " | property=" + property.Key
                    + "; final=" + DescribeValue(property.Value)
                    + "; sources=" + DescribeSources(property.Sources));
            }
        }

        /// <summary>压缩显示来源键、类别、原始值、运算符和应用状态。</summary>
        private static string DescribeSources(
            IReadOnlyList<ChipInformationProvenance> sources)
        {
            if (sources == null || sources.Count == 0)
            {
                return "<none>";
            }

            List<string> values = new List<string>();
            for (int index = 0; index < sources.Count; index++)
            {
                ChipInformationProvenance source = sources[index];
                if (source != null)
                {
                    values.Add(
                        (source.SourceKey ?? "<none>")
                        + ":" + source.ContributionKind
                        + ":" + DescribeValue(source.Value)
                        + ":op=" + (source.Operation ?? "<none>")
                        + (source.Applied ? ":applied" : ":ignored"));
                }
            }

            return string.Join(",", values.ToArray());
        }

        /// <summary>按固定格式压缩显示类型化值。</summary>
        private static string DescribeValue(ChipInformationValue value)
        {
            if (value == null)
            {
                return "<none>";
            }

            switch (value.Kind)
            {
                case ChipInformationValueKind.Text:
                    return value.Text ?? string.Empty;
                case ChipInformationValueKind.Boolean:
                    return value.Boolean.ToString();
                case ChipInformationValueKind.Integer:
                    return value.Integer.ToString(CultureInfo.InvariantCulture);
                case ChipInformationValueKind.Number:
                case ChipInformationValueKind.Percentage:
                case ChipInformationValueKind.Duration:
                    return value.Number.ToString("R", CultureInfo.InvariantCulture);
                case ChipInformationValueKind.Enum:
                    return value.DefinitionName ?? value.EnumKey ?? value.DisplayKey ?? string.Empty;
                case ChipInformationValueKind.List:
                    List<string> items = new List<string>();
                    for (int index = 0; index < value.Items.Count; index++)
                    {
                        items.Add(DescribeValue(value.Items[index]));
                    }

                    return string.Join("|", items.ToArray());
                default:
                    return string.Empty;
            }
        }
    }

    /// <summary>统一节点事实诊断结果。</summary>
    public sealed class ChipFeatureRawFactReport
    {
        /// <summary>按节点路径和属性来源排列的诊断行。</summary>
        public List<string> Lines { get; } = new List<string>();
    }
}
