using Verse;

namespace BDP.Core.CombatBody
{
    /// <summary>
    /// 战斗体主链宿主 Comp 配置。
    /// 它只提供主链最小参数，不承载战斗体具体表现业务。
    /// </summary>
    public sealed class CompProperties_CombatBodyHost : CompProperties
    {
        /// <summary>
        /// 战斗体崩解后的基础冷却时长。
        /// </summary>
        public int collapseCooldownTicks = 0;

        /// <summary>
        /// 战斗体开启后的每秒维持消耗。
        /// </summary>
        public float maintenanceDrainPerSecond = 0f;

        /// <summary>
        /// 构造战斗体宿主配置并绑定正式 Comp 类型。
        /// </summary>
        public CompProperties_CombatBodyHost()
        {
            compClass = typeof(CompCombatBodyHost);
        }
    }
}
