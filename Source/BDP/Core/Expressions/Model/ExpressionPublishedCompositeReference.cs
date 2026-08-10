using System.Collections.Generic;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 一条公开复合来源引用。
    /// 它只表达某条复合结果由哪些正式结果组成，不承载内部构建上下文。
    /// </summary>
    public sealed class ExpressionPublishedCompositeReference
    {
        /// <summary>
        /// 当前复合结果自身的稳定标识。
        /// </summary>
        public string CompositeId { get; internal set; }

        /// <summary>
        /// 当前复合结果的高层类型键。
        /// 它是内部复合类型的字符串投影，只服务公开读取判断。
        /// </summary>
        public string CompositeKindKey { get; internal set; }

        /// <summary>
        /// 当前复合结果引用的全部来源结果标识。
        /// </summary>
        public IReadOnlyList<string> SourceResultIds { get; internal set; }

        /// <summary>
        /// 当前复合结果的主侧来源结果标识。
        /// </summary>
        public string MainSourceResultId { get; internal set; }

        /// <summary>
        /// 当前复合结果的副侧来源结果标识。
        /// </summary>
        public string SubSourceResultId { get; internal set; }
    }
}
