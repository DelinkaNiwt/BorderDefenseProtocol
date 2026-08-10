namespace BDP.Core.Chips
{
    /// <summary>
    /// 芯片定义校验消息严重级别。
    /// </summary>
    internal enum ChipDefinitionValidationSeverity
    {
        /// <summary>
        /// 警告。
        /// 它表示芯片仍可被理解，但存在不理想之处。
        /// </summary>
        Warning,

        /// <summary>
        /// 错误。
        /// 它表示芯片未满足最低正式要求。
        /// </summary>
        Error
    }
}
