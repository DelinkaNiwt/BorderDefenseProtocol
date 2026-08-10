using System.Collections.Generic;
using Verse;

namespace BDP.Core.Semantics
{
    /// <summary>
    /// 统一语义上下文接口。
    /// 它只描述一份随过程流动的语义信息，不负责业务执行。
    /// </summary>
    public interface ISemanticContext
    {
        /// <summary>
        /// 这次过程自己的稳定标识。
        /// </summary>
        string Id { get; }

        /// <summary>
        /// 这次过程对外应显示的名称。
        /// </summary>
        string DisplayLabel { get; }

        /// <summary>
        /// 这次过程属于哪一类来源。
        /// </summary>
        SemanticSourceKind SourceKind { get; }

        /// <summary>
        /// 这次过程的原因标识。
        /// </summary>
        string ReasonKey { get; }

        /// <summary>
        /// 发起者。
        /// </summary>
        Thing Instigator { get; }

        /// <summary>
        /// 一组轻量标签。
        /// </summary>
        IReadOnlyList<string> Tags { get; }

        /// <summary>
        /// 少量扩展信息。
        /// </summary>
        IReadOnlyDictionary<string, string> ExtraData { get; }
    }
}
