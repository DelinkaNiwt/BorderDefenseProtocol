using System.Collections.Generic;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 一条高层合成结果的引用信息。
    /// 它只记录“这条高层结果由哪些下层结果参与形成”。
    /// </summary>
    internal sealed class CompositeExpressionReference
    {
        /// <summary>
        /// 当前高层结果自己的稳定标识。
        /// </summary>
        public string CompositeId { get; set; }

        /// <summary>
        /// 当前高层结果属于哪种高层类型。
        /// </summary>
        public CompositeExpressionKind CompositeKind { get; set; }

        /// <summary>
        /// 当前高层结果所引用的下层结果标识列表。
        /// </summary>
        public IReadOnlyList<string> SourceResultIds { get; set; }

        /// <summary>
        /// 当前高层结果引用的主侧源结果标识。
        /// 它服务双侧编排按主副语义稳定取值，不再依赖列表顺序约定。
        /// </summary>
        public string MainSourceResultId { get; set; }

        /// <summary>
        /// 当前高层结果引用的副侧源结果标识。
        /// 它服务双侧编排按主副语义稳定取值，不再依赖列表顺序约定。
        /// </summary>
        public string SubSourceResultId { get; set; }
    }
}
