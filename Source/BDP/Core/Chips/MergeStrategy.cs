namespace BDP.Core.Chips
{
    /// <summary>
    /// 任意配置覆盖层处理列表字段时使用的中性合并策略。
    /// </summary>
    public enum MergeStrategy
    {
        /// <summary>
        /// 覆盖层的列表追加到原列表之后（默认）。
        /// 适用于 RangedModules 等希望叠加的字段。
        /// </summary>
        Append = 0,

        /// <summary>
        /// 覆盖层的列表完全替换原列表。
        /// 适用于需要彻底改变集合内容的场景。
        /// </summary>
        Replace = 1
    }
}
