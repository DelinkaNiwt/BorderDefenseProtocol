using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// BDP爆炸投射物——继承原版Projectile_Explosive，支持光束拖尾 + 引导飞行（变化弹）。
    /// 保留完整爆炸行为，拖尾由BeamTrailConfig控制。
    ///
    /// 架构v2：每tick在TickInterval中创建一段BDPTrailSegment(prev→current)，
    /// 由BDPEffectMapComponent统一管理渲染和生命周期。
    /// 投射物销毁后，已创建的线段自然渐隐。
    ///
    /// 引导飞行：与 Bullet_BDP 完全相同的逻辑，通过 GuidedFlightController 组合复用。
    /// </summary>
    public class Projectile_ExplosiveBDP : Projectile_Explosive
    {
        /// <summary>上一tick的绘制位置，用于创建线段。</summary>
        private Vector3 previousPosition;

        /// <summary>拖尾Material缓存。</summary>
        private Material trailMat;

        /// <summary>引导飞行控制器（null=普通弹道）。</summary>
        private GuidedFlightController guidedController;

        /// <summary>配置缓存。</summary>
        private BeamTrailConfig cachedConfig;
        private bool configResolved;

        private BeamTrailConfig Config
        {
            get
            {
                if (!configResolved)
                {
                    cachedConfig = def.GetModExtension<BeamTrailConfig>();
                    configResolved = true;
                }
                return cachedConfig;
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            // 记录初始位置
            previousPosition = DrawPos;

            // 预缓存Material
            if (Config != null && Config.enabled)
            {
                trailMat = MaterialPool.MatFrom(
                    Config.trailTexPath,
                    ShaderDatabase.MoteGlow,
                    Config.trailColor);
            }
        }

        protected override void TickInterval(int delta)
        {
            // 记录移动前位置
            Vector3 prev = DrawPos;
            base.TickInterval(delta);

            // 创建拖尾线段：prev→当前位置
            if (Config != null && Config.enabled && trailMat != null)
            {
                var comp = BDPEffectMapComponent.GetInstance(Map);
                comp?.CreateSegment(
                    prev, DrawPos, trailMat, Config.trailColor,
                    Config.trailWidth, Config.segmentDuration,
                    Config.startOpacity, Config.decayTime, Config.decaySharpness);
            }

            // 更新previousPosition供后续使用
            previousPosition = DrawPos;
        }

        /// <summary>
        /// 初始化引导飞行——由 Verb 在 Launch 后调用。
        /// 将弹道重定向到第一个路径点，重算飞行时间。
        /// </summary>
        public void InitGuidedFlight(List<Vector3> waypoints)
        {
            if (waypoints == null || waypoints.Count < 2) return;
            guidedController = new GuidedFlightController(waypoints);
            destination = guidedController.CurrentWaypoint;
            ticksToImpact = Mathf.CeilToInt(StartingTicksToImpact);
            if (ticksToImpact < 1) ticksToImpact = 1;
        }

        /// <summary>
        /// 引导飞行核心：到达中间锚点时重置飞行参数，继续飞向下一段。
        /// </summary>
        protected override void ImpactSomething()
        {
            if (guidedController != null && guidedController.IsGuided
                && guidedController.TryAdvanceWaypoint())
            {
                origin = destination;
                destination = guidedController.CurrentWaypoint;
                ticksToImpact = Mathf.CeilToInt(StartingTicksToImpact);
                if (ticksToImpact < 1) ticksToImpact = 1;
                return;
            }
            base.ImpactSomething();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref guidedController, "guidedController");
        }
    }
}
