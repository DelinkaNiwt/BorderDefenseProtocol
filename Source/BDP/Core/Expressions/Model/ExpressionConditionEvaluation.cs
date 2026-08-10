using System.Collections.Generic;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 一条来源声明在当前上下文中的条件评估结果。
    /// 它只回答条件是否成立，不替代表达结果本身。
    /// </summary>
    internal sealed class ExpressionConditionEvaluation
    {
        /// <summary>
        /// 当前条件集合是否已成立。
        /// </summary>
        public bool IsSatisfied { get; set; }

        /// <summary>
        /// 当前是否存在尚未被正式解释的条件。
        /// 为 true 时，不应把该来源当成已正式成立。
        /// </summary>
        public bool HasUnknownConditions { get; set; }

        /// <summary>
        /// 当前评估附带的最小诊断信息。
        /// </summary>
        public IReadOnlyList<string> Notes { get; set; }
    }
}
