namespace BDP.Core.Chips
{
    /// <summary>
    /// 芯片对物理槽位的占用方式。
    /// 它只描述占槽结构，不承诺武器数量、攻击方式或视觉表现。
    /// </summary>
    public enum ChipSlotOccupancy
    {
        /// <summary>
        /// 作者尚未填写占用方式。
        /// 该值只用于缺失检测，不是合法的正式配置。
        /// </summary>
        Unspecified = 0,

        /// <summary>
        /// 只占据当前一个物理槽位。
        /// </summary>
        Single = 1,

        /// <summary>
        /// 同时占据主槽与副槽中编号相同的两个物理槽位。
        /// </summary>
        PairedHands = 2
    }
}
