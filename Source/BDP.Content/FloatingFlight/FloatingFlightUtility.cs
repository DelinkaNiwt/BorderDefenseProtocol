using Verse;

namespace BDP.Content.FloatingFlight
{
    /// <summary>
    /// 浮空业务共享的最小事实查询工具。
    /// </summary>
    public static class FloatingFlightUtility
    {
        /// <summary>判断 Pawn 是否实际拥有浮空 Hediff。</summary>
        public static bool IsActuallyFlying(Pawn pawn)
        {
            return pawn?.health?.hediffSet?.GetFirstHediffOfDef(
                FloatingFlightDefOf.BDP_Hediff_FloatingFlight) != null;
        }
    }
}
