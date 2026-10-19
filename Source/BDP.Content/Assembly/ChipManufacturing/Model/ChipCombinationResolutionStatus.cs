namespace BDP.Content.Assembly.ChipManufacturing.Model
{
    /// <summary>组合解析的两种稳定结果。</summary>
    public enum ChipCombinationResolutionStatus
    {
        /// <summary>全部来源存在且组合合法。</summary>
        Valid,
        /// <summary>当前无法确认组合合法，包括来源缺失或规则不兼容。</summary>
        Invalid
    }
}
