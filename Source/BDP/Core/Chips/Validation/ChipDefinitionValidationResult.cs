using System.Collections.Generic;

namespace BDP.Core.Chips
{
    /// <summary>
    /// 芯片定义最低合法性校验结果。
    /// </summary>
    internal sealed class ChipDefinitionValidationResult
    {
        /// <summary>
        /// 当前目标是否通过最低合法性校验。
        /// </summary>
        public bool IsValid;

        /// <summary>
        /// 当前目标的错误消息集合。
        /// </summary>
        public IReadOnlyList<ChipDefinitionValidationMessage> Errors;

        /// <summary>
        /// 当前目标的警告消息集合。
        /// </summary>
        public IReadOnlyList<ChipDefinitionValidationMessage> Warnings;
    }
}
