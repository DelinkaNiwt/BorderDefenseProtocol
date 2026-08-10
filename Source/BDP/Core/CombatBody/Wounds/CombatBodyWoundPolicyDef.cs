using Verse;

namespace BDP.Core.CombatBody.Wounds
{
    /// <summary>
    /// 战斗体伤口策略配置。
    /// 这里只承载玩法默认值；架构边界由运行时代码保护。
    /// </summary>
    public sealed class CombatBodyWoundPolicyDef : Def
    {
        /// <summary>
        /// 战斗体激活期间是否压制单个伤口自己的流血表现。
        /// </summary>
        public bool suppressIndividualBleeding = true;

        /// <summary>
        /// 是否启用伤口导致的 Trion 持续流失。
        /// 默认关闭，保持当前战斗体资源平衡不变。
        /// </summary>
        public bool trionDrainEnabled = false;

        /// <summary>
        /// 伤口 Trion 流失的默认计算口径。
        /// </summary>
        public CombatBodyWoundTrionDrainMetric trionDrainMetric = CombatBodyWoundTrionDrainMetric.RawBleedRate;

        /// <summary>
        /// 每 1.0 原版 rawBleedRate 对应的每秒 Trion 流失。
        /// </summary>
        public float trionDrainPerRawBleedRatePerSecond = 0f;

        /// <summary>
        /// 每 1 点原版伤口严重度对应的每秒 Trion 流失。
        /// </summary>
        public float trionDrainPerSeverityPerSecond = 0f;

        /// <summary>
        /// 是否把新鲜缺失部位的原版流血潜势纳入 Trion 流失口径。
        /// </summary>
        public bool includeMissingPartBleedPotential = true;

        /// <summary>
        /// 低频校准间隔。只在 Trion 伤口流失启用时使用。
        /// </summary>
        public int calibrationIntervalTicks = 600;

        /// <summary>
        /// 伤口没有继续发生变化后，Trion 流失保持的 tick 数。
        /// 默认 600 tick，即 RimWorld 游戏时间 10 秒。
        /// </summary>
        public int trionDrainIdleTimeoutTicks = 600;
    }
}
