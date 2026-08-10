namespace BDP.Core.CombatBody
{
    /// <summary>
    /// 战斗体外层阶段真值口。
    /// 这里只描述当前阶段、阶段查询与阶段切换，
    /// 不负责 Trion 资源细节，也不负责攻击、表现或界面。
    /// 它只服务 CombatBody 内部主链，不是对外正式开放面。
    /// </summary>
    internal interface ICombatBodyPhaseState : ICombatBodyReader
    {
        /// <summary>
        /// 启动一次手动形态切换后的短时准入锁。
        /// </summary>
        void BeginManualTransformLock(int lockTicks);

        void EnterActive(float allocatedTrion);

        void EnterCollapsing(string reason);

        void EnterCooldown(int cooldownTicks);

        void EnterInactive();
    }
}
