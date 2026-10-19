namespace BDP.Core.Information
{
    /// <summary>
    /// 来源对字段最终值产生的通用贡献方式。
    /// </summary>
    public enum ChipInformationContributionKind
    {
        /// <summary>基础值贡献。</summary>
        Base,

        /// <summary>变体来源贡献。</summary>
        Variant,

        /// <summary>运行结果贡献。</summary>
        Runtime,

        /// <summary>直接替换。</summary>
        Replacement,

        /// <summary>加法贡献。</summary>
        Addition,

        /// <summary>倍率贡献。</summary>
        Multiplier,

        /// <summary>通用派生贡献。</summary>
        Derived,

        /// <summary>来源被规则抑制。</summary>
        Suppressed,

        /// <summary>无法确认的贡献方式。</summary>
        Unknown
    }
}
