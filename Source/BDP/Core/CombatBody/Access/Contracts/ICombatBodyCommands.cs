namespace BDP.Core.CombatBody
{
    /// <summary>
    /// 战斗体正式请求面。
    /// 外部只能通过这层发起进入、离开或崩解请求，不能直接改相位真值。
    /// </summary>
    public interface ICombatBodyCommands
    {
        /// <summary>
        /// 尝试进入战斗体。
        /// </summary>
        bool TryActivate();

        /// <summary>
        /// 请求主动解除战斗体。
        /// </summary>
        void RequestRelease();

        /// <summary>
        /// 触发战斗体崩解。
        /// </summary>
        void TriggerCollapse(string reason);

        /// <summary>
        /// 在崩解表现结束后推进正式收尾。
        /// </summary>
        void FinalizeCollapse();
    }
}
