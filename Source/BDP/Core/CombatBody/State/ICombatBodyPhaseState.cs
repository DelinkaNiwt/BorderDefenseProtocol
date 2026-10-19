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

        /// <summary>记录当前会话真正锁定的资源量，不重新切换形态。</summary>
        void UpdateAllocatedTrion(float amount);

        /// <summary>进入激活阶段并记录实际锁定。</summary>
        void EnterActive(float allocatedTrion);

        /// <summary>进入崩解表现阶段。</summary>
        void EnterCollapsing(string reason);

        /// <summary>结束当前会话并进入冷却。</summary>
        void EnterCooldown(int cooldownTicks);

        /// <summary>回到非激活阶段。</summary>
        void EnterInactive();
    }
}
