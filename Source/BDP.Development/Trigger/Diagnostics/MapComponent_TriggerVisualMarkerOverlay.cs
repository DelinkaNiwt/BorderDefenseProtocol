using Verse;

namespace BDP.Development.Trigger.Diagnostics
{
    /// <summary>
    /// Trigger 视觉 marker 的地图级绘制组件。
    /// 它挂在 MapComponentDraw 上，因此不会再受“必须选中 Pawn”这一原版选择框流程限制。
    /// </summary>
    public sealed class MapComponent_TriggerVisualMarkerOverlay : MapComponent
    {
        /// <summary>
        /// 用当前地图初始化地图级 marker 绘制组件。
        /// </summary>
        public MapComponent_TriggerVisualMarkerOverlay(Map map) : base(map)
        {
        }

        /// <summary>
        /// 在地图绘制阶段为当前地图上的全部 Pawn 绘制 Trigger marker。
        /// 这里保留上帝模式和总开关约束，但不要求 Pawn 被选中。
        /// </summary>
        public override void MapComponentDraw()
        {
            if (!DebugSettings.godMode
                || !TriggerVisualMarkerSettings.OverlayEnabled
                || map == null
                || Find.CurrentMap != map
                || map.mapPawns == null
                || map.mapPawns.AllPawnsSpawned == null)
            {
                return;
            }

            for (int i = 0; i < map.mapPawns.AllPawnsSpawned.Count; i++)
            {
                Pawn pawn = map.mapPawns.AllPawnsSpawned[i];
                if (pawn == null
                    || pawn.Destroyed
                    || pawn.Map != map)
                {
                    continue;
                }

                TriggerVisualMarkerOverlayDrawer.DrawForPawn(pawn);
            }
        }
    }
}
