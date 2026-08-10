namespace BDP.Content.Assembly.ChipManufacturing.Model
{
    /// <summary>组合解析的三种稳定结果。</summary>
    public enum ChipCombinationResolutionStatus
    {
        /// <summary>全部来源存在且组合合法。</summary>
        Valid,
        /// <summary>至少一个来源 Def 暂时缺失。</summary>
        MissingSource,
        /// <summary>全部来源存在但组合明确非法。</summary>
        Invalid
    }
}
