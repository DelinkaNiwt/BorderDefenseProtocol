namespace BDP.Core.Expressions
{
    /// <summary>
    /// 公开表达读取面使用的四通道类别。
    /// 它只描述结果当前属于哪条发布通道，不承载内部构建细节。
    /// </summary>
    public enum ExpressionPublishedChannelKind
    {
        /// <summary>
        /// Verb 通道。
        /// </summary>
        Verb,

        /// <summary>
        /// Ability 通道。
        /// </summary>
        Ability,

        /// <summary>
        /// Hediff 通道。
        /// </summary>
        Hediff,

        /// <summary>
        /// Passive 通道。
        /// </summary>
        Passive
    }
}
