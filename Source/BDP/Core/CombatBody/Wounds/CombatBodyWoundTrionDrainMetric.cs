namespace BDP.Core.CombatBody.Wounds
{
    /// <summary>
    /// 战斗体伤口 Trion 流失的计算口径。
    /// </summary>
    public enum CombatBodyWoundTrionDrainMetric
    {
        /// <summary>
        /// 按原版伤口未压制前的流血率计算。
        /// </summary>
        RawBleedRate,

        /// <summary>
        /// 按原版伤口严重度计算。
        /// </summary>
        Severity
    }
}
