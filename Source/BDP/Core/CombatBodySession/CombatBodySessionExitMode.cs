namespace BDP.Core.CombatBodySession
{
    /// <summary>
    /// 战斗会话退出模式。
    /// 这里只表达跨系统事务的退出语义，不持有任何长期状态。
    /// </summary>
    internal enum CombatBodySessionExitMode
    {
        /// <summary>
        /// 玩家主动解除。
        /// </summary>
        Release,

        /// <summary>
        /// 被动崩解收尾。
        /// </summary>
        Collapse
    }
}

