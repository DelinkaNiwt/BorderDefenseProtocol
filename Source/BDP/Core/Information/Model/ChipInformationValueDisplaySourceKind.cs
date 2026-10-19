namespace BDP.Core.Information
{
    /// <summary>说明枚举值的玩家向文字应从何处取得。</summary>
    public enum ChipInformationValueDisplaySourceKind
    {
        /// <summary>保留原始枚举名或内部标识，不做翻译。</summary>
        Raw,

        /// <summary>按显式 Keyed（文本键）读取译名。</summary>
        TranslationKey,

        /// <summary>按 Def 类型和 DefName（定义键）读取 Def.LabelCap。</summary>
        DefReference
    }
}
