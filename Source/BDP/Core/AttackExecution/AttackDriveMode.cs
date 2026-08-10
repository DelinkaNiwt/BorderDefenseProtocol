namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 攻击编排的推进方式。
    /// 它只回答这次编排是即时完成，还是需要持续推进器承接。
    /// </summary>
    internal enum AttackDriveMode
    {
        /// <summary>
        /// 当前编排可在本次执行链中直接完成。
        /// </summary>
        Immediate = 0,

        /// <summary>
        /// 当前编排需要持续推进器跨时间承接。
        /// </summary>
        Continuous = 1
    }
}
