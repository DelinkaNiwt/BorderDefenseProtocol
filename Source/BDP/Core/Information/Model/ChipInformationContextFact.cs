using System.Collections.Generic;

namespace BDP.Core.Information
{
    /// <summary>
    /// 形态或姿态上下文的中性输入事实。
    /// </summary>
    public sealed class ChipInformationContextFact
    {
        /// <summary>稳定上下文键。</summary>
        public string ContextKey { get; private set; }

        /// <summary>上下文名称语言包键。</summary>
        public string LabelKey { get; private set; }

        /// <summary>父级上下文稳定键；一级形态为空，姿态指向所属形态。</summary>
        public string ParentContextKey { get; private set; }

        /// <summary>上下文包含的表达事实。</summary>
        public IReadOnlyList<ChipInformationExpressionFact> Expressions { get; private set; }

        /// <summary>创建上下文事实。</summary>
        public ChipInformationContextFact(
            string contextKey,
            string labelKey,
            IEnumerable<ChipInformationExpressionFact> expressions,
            string parentContextKey = null)
        {
            ContextKey = contextKey;
            LabelKey = labelKey;
            ParentContextKey = parentContextKey;
            List<ChipInformationExpressionFact> result = new List<ChipInformationExpressionFact>();
            if (expressions != null)
            {
                foreach (ChipInformationExpressionFact expression in expressions)
                {
                    if (expression != null)
                    {
                        result.Add(expression);
                    }
                }
            }

            Expressions = result.AsReadOnly();
        }
    }
}
