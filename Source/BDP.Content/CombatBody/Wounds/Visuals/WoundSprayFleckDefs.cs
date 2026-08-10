using Verse;

namespace BDP.Content.CombatBody.Wounds.Visuals
{
    /// <summary>
    /// 战斗体伤口喷射使用的 FleckDef 静态引用。
    /// </summary>
    [StaticConstructorOnStartup]
    internal static class WoundSprayFleckDefs
    {
        /// <summary>
        /// 核心高亮粒子。
        /// </summary>
        internal static readonly FleckDef LeakCore =
            DefDatabase<FleckDef>.GetNamed("BDP_Fleck_LeakCore");

        /// <summary>
        /// 主体黄绿粒子。
        /// </summary>
        internal static readonly FleckDef LeakMid =
            DefDatabase<FleckDef>.GetNamed("BDP_Fleck_LeakMid");

        /// <summary>
        /// 外层扩散粒子。
        /// </summary>
        internal static readonly FleckDef LeakOuter =
            DefDatabase<FleckDef>.GetNamed("BDP_Fleck_LeakOuter");
    }
}
