namespace BDP.Core.Combos
{
    /// <summary>
    /// 单条组合技定义校验消息。
    /// 它只描述哪一项不合法，不承担修复动作。
    /// </summary>
    internal sealed class ComboDefinitionValidationMessage
    {
        /// <summary>
        /// 当前消息的稳定错误码。
        /// </summary>
        public string Code;

        /// <summary>
        /// 当前消息对应的来源芯片 DefName。
        /// 只有依赖校验相关消息才会填写。
        /// </summary>
        public string SourceChipDefName;

        /// <summary>
        /// 当前消息对应的字段名。
        /// 只有字段级失效时才会填写。
        /// </summary>
        public string FieldName;

        /// <summary>
        /// 当前消息的正文。
        /// </summary>
        public string Message;
    }
}
