namespace BDP.Core.CombatBody
{
    /// <summary>
    /// 战斗体正式只读面。
    /// 这一面只暴露战斗体外层相位 owner 自己持有的事实。
    /// </summary>
    public interface ICombatBodyReader
    {
        /// <summary>
        /// 当前战斗体外层相位。
        /// </summary>
        CombatBodyPhase Phase { get; }

        /// <summary>
        /// 当前战斗体正式锁定的 Trion 量。
        /// </summary>
        float AllocatedTrion { get; }

        /// <summary>
        /// 当前进入 Active 的绝对 tick。
        /// </summary>
        int ActivationTick { get; }

        /// <summary>
        /// 当前崩解原因。
        /// </summary>
        string CollapseReason { get; }

        /// <summary>
        /// 现在是否允许再次激活战斗体。
        /// </summary>
        bool CanActivate();

        /// <summary>
        /// 现在是否允许玩家手动关闭战斗体。
        /// </summary>
        bool CanManualDeactivate();

        /// <summary>
        /// 当前是否处于崩解表现中的无敌阶段。
        /// </summary>
        bool IsInvulnerable();

        /// <summary>
        /// 冷却剩余 tick。
        /// </summary>
        int GetCooldownRemaining();

        /// <summary>
        /// 崩解表现剩余 tick。
        /// </summary>
        int GetCollapseRemaining();
    }
}
