namespace BDP.Core.CombatBody
{
    /// <summary>
    /// 战斗体正式请求面。
    /// 外部只能通过这层发起进入、离开或崩解请求，不能直接改相位真值。
    /// </summary>
    public interface ICombatBodyCommands
    {
        /// <summary>
        /// 系统尝试进入战斗体；检查阶段、资格和资源，不检查或启动玩家手动切换锁。
        /// </summary>
        bool TryActivate();

        /// <summary>玩家尝试激活战斗体；成功后启动手动切换锁。</summary>
        bool TryActivateManually();

        /// <summary>
        /// 请求主动解除战斗体。
        /// </summary>
        void RequestRelease();

        /// <summary>
        /// 因 Trion 腺体资格失效而强制执行普通解除。
        /// 该入口只供系统状态修正，不作为玩家按钮。
        /// </summary>
        void ForceReleaseForEligibilityLoss();

        /// <summary>装备卸下时结束活跃战斗体，不受手动锁限制，也不覆盖正在进行的崩解。</summary>
        void ReleaseForEquipmentDetach();

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
