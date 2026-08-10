using System.Collections.Generic;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 单枚活跃芯片的定义层诊断结果。
    /// 它只说明主模组是否正式接受这枚芯片定义。
    /// </summary>
    internal sealed class ChipDefinitionDiagnosticEntry
    {
        /// <summary>
        /// 当前芯片实例标识。
        /// </summary>
        public string ChipThingId { get; set; }

        /// <summary>
        /// 当前芯片显示名。
        /// </summary>
        public string ChipLabel { get; set; }

        /// <summary>
        /// 当前芯片是否通过最低合法性校验。
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 当前芯片的错误消息集合。
        /// </summary>
        public IReadOnlyList<string> Errors { get; set; }

        /// <summary>
        /// 当前芯片的警告消息集合。
        /// </summary>
        public IReadOnlyList<string> Warnings { get; set; }
    }
}
