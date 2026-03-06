namespace BDP.Trigger
{
    /// <summary>
    /// 芯片槽位限制类型。
    /// 定义芯片可以插入哪些类型的槽位。
    /// </summary>
    public enum ChipSlotRestriction
    {
        /// <summary>无限制，可插任何槽位（左手、右手、特殊）。</summary>
        None = 0,

        /// <summary>只能插特殊槽位（Special）。</summary>
        SpecialOnly = 1,

        /// <summary>只能插左右手槽位（LeftHand/RightHand），不能插特殊槽位。</summary>
        HandsOnly = 2
    }
}
