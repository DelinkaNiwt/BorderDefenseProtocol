namespace BDP.Core.VerbHosting
{
    /// <summary>
    /// BDP 正式宿主 Verb 使用的固定槽位身份。
    /// 这些槽位只表达原版 VerbTracker 可稳定持有的入口身份，不承载业务真值。
    /// </summary>
    internal enum BdpFormalVerbHostSlot
    {
        /// <summary>
        /// 当前结果不命中任何正式宿主槽位。
        /// </summary>
        None = 0,

        /// <summary>
        /// 主侧主攻击槽位。
        /// </summary>
        MainPrimary,

        /// <summary>
        /// 主侧副攻击槽位。
        /// </summary>
        MainSecondary,

        /// <summary>
        /// 副侧主攻击槽位。
        /// </summary>
        SubPrimary,

        /// <summary>
        /// 副侧副攻击槽位。
        /// </summary>
        SubSecondary,

        /// <summary>
        /// 双武器主攻击槽位。
        /// </summary>
        DualPrimary,

        /// <summary>
        /// 双武器副攻击槽位。
        /// </summary>
        DualSecondary,

        /// <summary>
        /// 组合技主攻击槽位。
        /// </summary>
        ComboPrimary,

        /// <summary>
        /// 组合技副攻击槽位。
        /// </summary>
        ComboSecondary
    }
}
