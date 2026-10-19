using Verse;
using Verse.AI;

namespace BDP.Content.FloatingFlight.Pathing
{
    /// <summary>
    /// 始终浮空使用的纯飞行路径网格。
    /// </summary>
    public sealed class PathGrid_FloatingAlways : PathGrid
    {
        /// <summary>按地图和 XML（定义文件）网格定义创建纯飞行网格。</summary>
        public PathGrid_FloatingAlways(Map map, PathGridDef def)
            : base(map, def)
        {
        }

        /// <summary>屋顶全部阻断，露天使用空中代价。</summary>
        public override int CalculatedCostAt(
            IntVec3 c,
            bool perceivedStatic,
            IntVec3 prevCell,
            int? baseCostOverride = null)
        {
            return c.Roofed(map)
                ? ImpassableCost
                : FloatingFlightPathUtility.CalculateOpenAirCost(
                    map,
                    c,
                    perceivedStatic);
        }
    }
}
