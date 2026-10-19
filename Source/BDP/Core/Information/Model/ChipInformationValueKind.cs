namespace BDP.Core.Information
{
    /// <summary>
    /// 芯片信息字段的稳定值类型。
    /// </summary>
    public enum ChipInformationValueKind
    {
        /// <summary>普通文本。</summary>
        Text,

        /// <summary>布尔值。</summary>
        Boolean,

        /// <summary>整数。</summary>
        Integer,

        /// <summary>普通数值。</summary>
        Number,

        /// <summary>百分比数值，内部仍保存 0～100 的数值。</summary>
        Percentage,

        /// <summary>持续时间，内部保存标准秒数。</summary>
        Duration,

        /// <summary>稳定枚举键。</summary>
        Enum,

        /// <summary>有序值列表。</summary>
        List,

        /// <summary>带稳定字段键的结构化记录。</summary>
        Record
    }
}
