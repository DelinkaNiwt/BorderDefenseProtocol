namespace BDP.Core.Chips
{
    /// <summary>
    /// 芯片所属的槽位区域。
    /// 它只区分主副槽区和特殊槽区，不描述具体槽位数量或占用方式。
    /// </summary>
    public enum ChipSlotRegion
    {
        /// <summary>
        /// 作者尚未填写槽位区域。
        /// 该值只用于缺失检测，不是合法的正式配置。
        /// </summary>
        Unspecified = 0,

        /// <summary>
        /// 主槽与副槽共同组成的主副槽区。
        /// </summary>
        MainSub = 1,

        /// <summary>
        /// 特殊槽区。
        /// </summary>
        Special = 2
    }
}
