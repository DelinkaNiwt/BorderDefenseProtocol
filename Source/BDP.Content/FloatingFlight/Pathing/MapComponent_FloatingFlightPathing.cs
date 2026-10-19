using Verse;
using Verse.AI;

namespace BDP.Content.FloatingFlight.Pathing
{
    /// <summary>沿原版地图事件刷新两套 BDP 浮空网格，不拦截全局屋顶设置方法。</summary>
    public sealed class MapComponent_FloatingFlightPathing : MapComponent
    {
        /// <summary>地图完成组件构造后订阅真实屋顶变化；原版同值写入不会发布事件。</summary>
        public MapComponent_FloatingFlightPathing(Map map) : base(map)
        {
            map.events.RoofChanged += OnRoofChanged;
        }

        /// <summary>地图移除时退订；重复清理安全，不留下已释放网格的事件消费者。</summary>
        public override void MapRemoved()
        {
            map.events.RoofChanged -= OnRoofChanged;
        }

        /// <summary>屋顶变化只刷新 BDP 网格，共享原版一次区域通知，不改变普通网格代价。</summary>
        private void OnRoofChanged(IntVec3 cell)
        {
            if (map.pathing == null || !cell.InBounds(map))
            {
                return;
            }

            bool haveNotified = false;
            Recalculate(FloatingFlightDefOf.BDP_PathGrid_FloatingHybrid, cell, ref haveNotified);
            Recalculate(FloatingFlightDefOf.BDP_PathGrid_FloatingAlways, cell, ref haveNotified);
        }

        /// <summary>只访问指定 BDP 网格；地图释放网格后不再向已释放数组写入。</summary>
        private void Recalculate(PathGridDef def, IntVec3 cell, ref bool haveNotified)
        {
            if (def == null)
            {
                return;
            }

            PathGrid grid = map.pathing.Get(def)?.pathGrid;
            if (grid != null && grid.Grid_Unsafe.IsCreated)
            {
                grid.RecalculatePerceivedPathCostAt(cell, ref haveNotified);
            }
        }
    }
}
