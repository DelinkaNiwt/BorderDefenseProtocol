using System.Collections.Generic;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 单枚芯片契约诊断条目。
    /// 它只描述主模组是否接受了这枚芯片的契约，
    /// 不泄漏内部解析过程对象。
    /// </summary>
    internal sealed class ExpressionContractDiagnosticEntry
    {
        /// <summary>
        /// 当前诊断对应的芯片实例标识。
        /// </summary>
        public string ChipThingId { get; set; }

        /// <summary>
        /// 当前诊断对应的芯片显示名。
        /// </summary>
        public string ChipLabel { get; set; }

        /// <summary>
        /// 当前芯片契约是否通过最小校验。
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 当前芯片契约正式承认的条目数量。
        /// </summary>
        public int AcceptedEntryCount { get; set; }

        /// <summary>
        /// 当前芯片契约的错误列表。
        /// </summary>
        public IReadOnlyList<string> Errors { get; set; }

        /// <summary>
        /// 当前芯片契约的警告列表。
        /// </summary>
        public IReadOnlyList<string> Warnings { get; set; }
    }
}
