namespace BDP.Core.Chips
{
    /// <summary>
    /// 单条芯片定义校验消息。
    /// 它应保持面向开发者和玩家都能读懂的表述。
    /// </summary>
    internal sealed class ChipDefinitionValidationMessage
    {
        /// <summary>
        /// 当前消息的稳定代码。
        /// </summary>
        public string Code;

        /// <summary>
        /// 当前消息的严重级别。
        /// </summary>
        public ChipDefinitionValidationSeverity Severity;

        /// <summary>
        /// 当前消息对应的正式声明块。
        /// </summary>
        public ChipDefinitionDeclaredBlock? Block;

        /// <summary>
        /// 当前消息的可读说明文本。
        /// </summary>
        public string Message;
    }
}
