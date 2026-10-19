using System.Collections.Generic;
using Verse;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 一条表达来源成立条件配置。
    /// 标准条件由核心解释器求值，业务子类可提供自己的中性准入结果。
    /// </summary>
    public class ExpressionSourceConditionConfig
    {
        /// <summary>
        /// 当前条件的类型键。
        /// </summary>
        public string ConditionKey;

        /// <summary>
        /// 当前条件是否为必需条件。
        /// </summary>
        public bool Required = true;

        /// <summary>
        /// 当前条件附带的轻量参数。
        /// 第一版先保留字符串参数位。
        /// </summary>
        public List<string> Parameters;

        /// <summary>业务子类的求值入口；空结果表示继续使用既有标准条件解释器。</summary>
        public virtual bool? Evaluate(Pawn pawn, Thing sourceChip) { return null; }
    }
}
