namespace BDP.Core.Trion
{
    /// <summary>
    /// Trion 资源系统正式请求面。
    /// 外部系统只能通过这层向资源 owner 提交请求，不能绕开 owner 改资源真值。
    /// </summary>
    public interface ITrionCommands
    {
        /// <summary>
        /// 只检查够不够，不做扣减。
        /// </summary>
        bool CanAfford(float cost);

        /// <summary>
        /// 全额扣除或完全不扣。
        /// </summary>
        bool TryConsume(float cost);

        /// <summary>
        /// 尽力消耗，直到可用量见底。
        /// </summary>
        void ConsumeUntilDepleted(float amount);

        /// <summary>
        /// 设置新的预占用量。
        /// </summary>
        void SetReserved(float amount);

        /// <summary>
        /// 把一部分可用量正式转成锁定量。
        /// </summary>
        bool Allocate(float amount);

        /// <summary>
        /// 释放正式锁定量。
        /// </summary>
        void Release(float amount);

        /// <summary>
        /// 注册一条持续消耗，单位为 Trion/秒。
        /// </summary>
        void RegisterDrain(TrionDrainKey key, float perSecond);

        /// <summary>
        /// 注销一条持续消耗。
        /// </summary>
        void UnregisterDrain(TrionDrainKey key);

        /// <summary>
        /// 设置是否冻结自然恢复。
        /// </summary>
        void SetFrozen(bool frozen);

        /// <summary>
        /// 按增减量调整当前值。
        /// 这是正式允许的调试写入口，仍然必须遵守 owner 的下界与上界约束。
        /// </summary>
        TrionCurrentWriteResult AdjustCurrent(float delta);

        /// <summary>
        /// 尝试直接把当前值设成指定目标值。
        /// 这是正式允许的调试写入口，仍然必须遵守 owner 的下界与上界约束。
        /// </summary>
        TrionCurrentWriteResult TrySetCurrent(float target);

    }
}
