using System.Collections.Generic;

namespace BDP.Core.Information
{
    /// <summary>
    /// 解析根级结构化事实块的通用规则。
    /// </summary>
    internal sealed class StructuredFactBlockInformationRule : IChipInformationRule
    {
        /// <summary>规则稳定键。</summary>
        public string RuleKey => "structured.fact.blocks";

        /// <summary>判断是否存在根级结构化事实块。</summary>
        public bool CanApply(ChipInformationRuleContext context)
        {
            return context?.Input?.Facts?.StructuredBlocks != null
                && context.Input.Facts.StructuredBlocks.Count > 0;
        }

        /// <summary>声明根级结构化事实块。</summary>
        public IReadOnlyList<ChipInformationBlockSchema> DescribeSchema(
            ChipInformationRuleContext context)
        {
            return CanApply(context)
                ? ChipInformationRuleSupport.DescribeFactBlocks(
                    context.Input.Facts.StructuredBlocks)
                : new List<ChipInformationBlockSchema>();
        }

        /// <summary>解析根级结构化事实块。</summary>
        public IReadOnlyList<ChipInformationContribution> Resolve(
            ChipInformationRuleContext context)
        {
            return CanApply(context)
                ? ChipInformationRuleSupport.ResolveFactBlocks(
                    context.Input.Facts.StructuredBlocks)
                : new List<ChipInformationContribution>();
        }
    }
}
