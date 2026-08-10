using System.Collections.Generic;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 一条表达来源成立条件配置。
    /// 第一版只保留结构位，后续再由条件解释层决定其正式语义。
    /// </summary>
    public sealed class ExpressionSourceConditionConfig
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
    }
}
