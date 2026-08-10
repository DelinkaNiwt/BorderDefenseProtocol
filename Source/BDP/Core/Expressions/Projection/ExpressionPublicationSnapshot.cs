using System.Collections.Generic;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 表达发布观察快照。
    /// 它只服务诊断和说明投影，不作为运行时真值来源。
    /// </summary>
    internal sealed class ExpressionPublicationSnapshot
    {
        /// <summary>
        /// 当前快照收集到的全部发布条目。
        /// </summary>
        public IReadOnlyList<ExpressionPublicationEntry> Entries { get; set; }
    }

    /// <summary>
    /// 一条表达发布观察条目。
    /// 它描述某条正式结果当前会被哪条发布通道消费。
    /// </summary>
    internal sealed class ExpressionPublicationEntry
    {
        /// <summary>
        /// 当前条目对应的正式结果标识。
        /// </summary>
        public string ResultId { get; set; }

        /// <summary>
        /// 当前条目对应的正式结果大类。
        /// </summary>
        public ExpressionResultKind ResultKind { get; set; }

        /// <summary>
        /// 当前条目发布到下游时使用的键。
        /// 没有稳定发布键时允许为空。
        /// </summary>
        public string PublishedKey { get; set; }

        /// <summary>
        /// 当前条目是否已经具备最小发布条件。
        /// 它只表示结构上的可发布，不代替下游运行结果。
        /// </summary>
        public bool IsPublished { get; set; }

        /// <summary>
        /// 当前条目若来自复合结果，则这里保留其来源结果标识。
        /// </summary>
        public IReadOnlyList<string> SourceResultIds { get; set; }
    }
}
