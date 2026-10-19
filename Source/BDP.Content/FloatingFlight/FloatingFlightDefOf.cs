using RimWorld;
using Verse;

namespace BDP.Content.FloatingFlight
{
    /// <summary>
    /// 浮空业务使用的原版 DefOf（定义快捷引用）。
    /// </summary>
    [DefOf]
    public static class FloatingFlightDefOf
    {
        /// <summary>实际承载飞行状态的 Hediff。</summary>
        public static HediffDef BDP_Hediff_FloatingFlight;

        /// <summary>仅移动时浮空使用的混合路径网格。</summary>
        public static PathGridDef BDP_PathGrid_FloatingHybrid;

        /// <summary>始终浮空使用的纯飞行路径网格。</summary>
        public static PathGridDef BDP_PathGrid_FloatingAlways;

        /// <summary>确保原版在静态初始化阶段填充定义引用。</summary>
        static FloatingFlightDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(FloatingFlightDefOf));
        }
    }
}
