namespace BDP.Content.CombatBody.Escape
{
    /// <summary>
    /// 紧急脱离徽标状态。
    /// 它只表达当前玩家可见的装载与就绪结果，不持有运行时真值。
    /// </summary>
    internal enum CombatBodyEmergencyEscapeBadgeState
    {
        /// <summary>
        /// 当前没有搭载声明紧急脱离能力的芯片。
        /// </summary>
        NotInstalled = 0,

        /// <summary>
        /// 已搭载紧急脱离芯片，但正式被动表达尚未可用。
        /// </summary>
        InstalledNotReady = 1,

        /// <summary>
        /// 已搭载紧急脱离芯片，且正式被动表达已经可用。
        /// </summary>
        Ready = 2
    }
}
