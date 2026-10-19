using System.Collections.Generic;

namespace BDP.Core.Information
{
    /// <summary>
    /// 直接公开一个中性事实集合的通用规则基类。
    /// </summary>
    internal abstract class FactInformationRuleBase : IChipInformationRule
    {
        /// <summary>规则稳定键。</summary>
        public abstract string RuleKey { get; }

        /// <summary>目标块键。</summary>
        protected abstract string BlockKey { get; }

        /// <summary>目标块语义类型。</summary>
        protected abstract string BlockKind { get; }

        /// <summary>目标块标题语言包键。</summary>
        protected abstract string BlockLabelKey { get; }

        /// <summary>目标块排序值。</summary>
        protected abstract int BlockOrder { get; }

        /// <summary>取得本规则消费的事实字段。</summary>
        protected abstract IEnumerable<ChipInformationFactValue> GetFields(
            ChipInformationFacts facts);

        /// <summary>判断是否有本规则能力。</summary>
        public bool CanApply(ChipInformationRuleContext context)
        {
            return context != null
                && context.Input != null
                && context.Input.Facts != null
                && ChipInformationRuleSupport.HasFields(
                    GetFields(context.Input.Facts));
        }

        /// <summary>声明事实字段。</summary>
        public IReadOnlyList<ChipInformationBlockSchema> DescribeSchema(
            ChipInformationRuleContext context)
        {
            if (!CanApply(context))
            {
                return new List<ChipInformationBlockSchema>();
            }

            return new[]
            {
                new ChipInformationBlockSchema(
                    BlockKey,
                    BlockKind,
                    BlockLabelKey,
                    BlockOrder,
                    ChipInformationRuleSupport.DescribeFields(
                        GetFields(context.Input.Facts)))
            };
        }

        /// <summary>解析事实字段。</summary>
        public IReadOnlyList<ChipInformationContribution> Resolve(
            ChipInformationRuleContext context)
        {
            if (!CanApply(context))
            {
                return new List<ChipInformationContribution>();
            }

            return ChipInformationRuleSupport.ResolveFields(
                BlockKey,
                BlockKind,
                BlockLabelKey,
                BlockOrder,
                GetFields(context.Input.Facts));
        }
    }
}
