using Verse;

namespace ProjectTrion.Components
{
    /// <summary>
    /// Trion能量管理组件的属性定义。
    /// 在XML Def中定义，用于初始化CompTrion组件。
    ///
    /// CompTrion properties defined in XML defs.
    /// </summary>
    public class CompProperties_Trion : CompProperties
    {
        public CompProperties_Trion()
        {
            this.compClass = typeof(CompTrion);
        }

        /// <summary>
        /// 策略类的全限定名。
        /// 例如：ApplicationLayer.Strategy_HumanCombatBody
        ///
        /// Full qualified name of the lifecycle strategy class.
        /// Example: ApplicationLayer.Strategy_HumanCombatBody
        /// </summary>
        public string strategyClassName = "";

        /// <summary>
        /// Trion总容量。
        /// 个体固定属性，当前设计不可提升。
        ///
        /// Total Trion capacity.
        /// Fixed per individual, cannot be upgraded in current design.
        /// </summary>
        public float capacity = 1000f;

        /// <summary>
        /// Trion自然恢复速率（每60Tick）。
        /// Natural Trion recovery rate per 60 ticks.
        /// </summary>
        public float recoveryRate = 2.0f;

        /// <summary>
        /// 基础泄漏速率（每60Tick）。
        /// Base Trion leak rate per 60 ticks (from injuries, etc.)
        /// </summary>
        public float leakRate = 0.5f;

        /// <summary>
        /// 基础维持消耗（每60Tick）。
        /// Base maintenance consumption per 60 ticks.
        /// 战斗体存在时的固定消耗。
        /// </summary>
        public float baseMaintenance = 1.0f;

        /// <summary>
        /// 是否在战斗体激活时冻结宿主的生理需求。
        /// Whether to freeze host's physiological needs when combat body is active.
        /// </summary>
        public bool freezePhysiologyInCombat = true;

        /// <summary>
        /// 受伤转化率（伤害值转Trion消耗）。
        /// Injury to Trion conversion rate (damage to consumption ratio).
        /// 默认1:1转化。
        /// Default 1:1 conversion.
        /// </summary>
        public float damageToTrionConversion = 1.0f;

        /// <summary>
        /// 是否启用虚拟伤害系统。
        /// Whether to enable virtual damage system.
        /// 启用后，战斗体在激活时不会受到物理伤害。
        /// When enabled, combat body takes no physical damage while active.
        /// </summary>
        public bool enableVirtualDamage = true;

        /// <summary>
        /// 是否启用快照与回滚机制。
        /// Whether to enable snapshot and rollback system.
        /// </summary>
        public bool enableSnapshot = true;

        /// <summary>
        /// 是否启用Bail Out系统。
        /// Whether to enable Bail Out (emergency teleport) system.
        /// </summary>
        public bool enableBailOut = true;
    }
}
