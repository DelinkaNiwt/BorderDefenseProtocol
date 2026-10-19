using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Content.Factions.GateArrival
{
    /// <summary>各门可分散在地图不同位置，独立于控制物本身的视野裁剪来绘制。</summary>
    public sealed class MapComponent_GateArrivalDrawing : MapComponent
    {
        /// <summary>原版绘制批次复用属性块，不生成每帧材质实例。</summary>
        private readonly DrawBatch batch = new DrawBatch();

        /// <summary>由原版地图部件机制创建。</summary>
        public MapComponent_GateArrivalDrawing(Map map) : base(map) { }

        /// <summary>两层原版黑洞：内部流动面和外圈折射边缘。</summary>
        public override void MapComponentDraw()
        {
            int now = Find.TickManager.TicksGame;
            foreach (Thing_GateArrival wave in GateArrivalHooks.ActiveWaves(map))
            {
                var gates = wave.Gates;
                for (int i = 0; i < gates.Count; i++)
                {
                    GateArrivalPoint gate = gates[i];
                    if (!gate.IsOpen || gate.position.Fogged(map)) continue;
                    float age = GateArrivalSchedule.VirtualAge(now, gate.openedTick, gate.closingTick, wave.Settings);
                    float id = (Gen.HashCombineInt(wave.thingIDNumber, i) & 0xFFFFFF) / 16777216f;
                    DrawLayer(FleckDefOf.PsycastSkipInnerExit, gate.position, wave.Settings.size, age, id);
                    DrawLayer(FleckDefOf.PsycastSkipOuterRingExit, gate.position, wave.Settings.size, age, id);
                }
            }
            batch.Flush();
        }

        /// <summary>只传特效年龄和稳定对象标识，不更改原版共享材质或 Def。</summary>
        private void DrawLayer(FleckDef fleck, IntVec3 position, float size, float age, float id)
        {
            var graphic = (Graphic_Fleck)fleck.graphicData.Graphic;
            graphic.DrawFleck(new FleckDrawData
            {
                pos = position.ToVector3ShiftedWithAltitude(fleck.altitudeLayer),
                scale = Vector3.one * size,
                color = Color.white,
                alpha = 1f,
                ageSecs = age,
                id = id
            }, batch);
        }

    }
}
