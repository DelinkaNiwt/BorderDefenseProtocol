using Verse;
using Verse.AI;

namespace BDP.Content.FloatingFlight.Pathing
{
    /// <summary>
    /// 仅移动时浮空使用的混合路径网格。
    /// </summary>
    public sealed class PathGrid_FloatingHybrid : PathGrid
    {
        /// <summary>按地图和 XML（定义文件）网格定义创建混合网格。</summary>
        public PathGrid_FloatingHybrid(Map map, PathGridDef def)
            : base(map, def)
        {
        }

        /// <summary>露天使用空中代价，屋顶委托原版地面规则。</summary>
        public override int CalculatedCostAt(
            IntVec3 c,
            bool perceivedStatic,
            IntVec3 prevCell,
            int? baseCostOverride = null)
        {
            return c.Roofed(map)
                ? map.pathing.Normal.pathGrid.CalculatedCostAt(
                    c,
                    perceivedStatic,
                    prevCell,
                    baseCostOverride)
                : FloatingFlightPathUtility.CalculateOpenAirCost(
                    map,
                    c,
                    perceivedStatic);
        }
    }
}
