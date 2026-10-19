using RimWorld;
using UnityEngine;
using Verse;
using BDP.Content.GroundDash;

namespace BDP.Content.Grasshopper
{
    /// <summary>
    /// 蚱蜢飞行器。
    ///
    /// 地面位移、单调进度曲线、阴影和落地回调由中性设施提供；
    /// 本类只保留蚱蜢踏板与气浪等专属视觉。
    /// </summary>
    public class PawnFlyer_Grasshopper : PawnFlyer_GroundDash
    {
        /// <summary>是否显示蚱蜢专属起跳踏板和气浪。</summary>
        protected virtual bool ShowGrasshopperTrailFx => true;

        /// <summary>起跳踏板是否已生成。</summary>
        private bool launchEffectSpawned;

        /// <summary>起点气浪是否已生成。</summary>
        private bool trailSpawned_0;

        /// <summary>5% 位置气浪是否已生成。</summary>
        private bool trailSpawned_5;

        /// <summary>15% 位置气浪是否已生成。</summary>
        private bool trailSpawned_15;

        /// <summary>生成蚱蜢专属踏板和气浪。</summary>
        protected override void Tick()
        {
            Vector3 trailPosition = DrawPos;
            base.Tick();

            if (!ShowGrasshopperTrailFx || Map == null)
            {
                return;
            }

            SpawnLaunchEffects();
            SpawnTrailEffects(trailPosition);
        }

        /// <summary>在起跳位置生成蚱蜢踏板和脉冲波纹。</summary>
        private void SpawnLaunchEffects()
        {
            if (launchEffectSpawned || ticksFlying < 1)
            {
                return;
            }

            launchEffectSpawned = true;
            Vector3 footPosition = new Vector3(
                startVec.x,
                AltitudeLayer.Shadows.AltitudeFor(),
                startVec.z - 0.3f);

            FleckDef platformDef = DefDatabase<FleckDef>.GetNamed("BDP_GrasshopperPlatform");
            if (platformDef != null)
            {
                FleckMaker.Static(footPosition, Map, platformDef, 0.7f);
            }

            FleckDef pulseDef = DefDatabase<FleckDef>.GetNamed("BDP_GrasshopperPulse");
            if (pulseDef == null)
            {
                return;
            }

            for (int i = 0; i < 2; i++)
            {
                FleckCreationData data = FleckMaker.GetDataStatic(
                    footPosition,
                    Map,
                    pulseDef,
                    1.2f);
                data.solidTimeOverride = 0.03f + i * 0.05f;
                Map.flecks.CreateFleck(data);
            }
        }

        /// <summary>在蚱蜢专属的三个控制点生成气浪。</summary>
        private void SpawnTrailEffects(Vector3 trailPosition)
        {
            FleckDef airPuffDef = DefDatabase<FleckDef>.GetNamed("BDP_GrasshopperAirPuff");
            if (airPuffDef == null)
            {
                return;
            }

            float progress = EvaluateProgress((float)ticksFlying / Mathf.Max(1, ticksFlightTime));
            if (!trailSpawned_0)
            {
                trailSpawned_0 = true;
                FleckMaker.Static(trailPosition, Map, airPuffDef, 1.5f);
            }
            else if (!trailSpawned_5 && progress >= 0.05f)
            {
                trailSpawned_5 = true;
                FleckMaker.Static(trailPosition, Map, airPuffDef, 1.5f);
            }
            else if (!trailSpawned_15 && progress >= 0.15f)
            {
                trailSpawned_15 = true;
                FleckMaker.Static(trailPosition, Map, airPuffDef, 1.5f);
            }
        }
    }
}
