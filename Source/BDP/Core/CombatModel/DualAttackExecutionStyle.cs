namespace BDP.Core.CombatModel
{
    /// <summary>
    /// 双侧复合攻击的正式执行风格。
    /// 它只描述双侧如何调度，不持有运行时状态。
    /// </summary>
    public sealed class DualAttackExecutionStyle
    {
        /// <summary>
        /// 当前双侧复合结果的正式调度方式。
        /// </summary>
        public DualExecutionSchedule Schedule;

        /// <summary>
        /// 构造当前执行风格的浅复制对象。
        /// </summary>
        public DualAttackExecutionStyle Clone()
        {
            return new DualAttackExecutionStyle
            {
                Schedule = Schedule
            };
        }
    }
}
