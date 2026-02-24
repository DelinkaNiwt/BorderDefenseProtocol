using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// BDP普通子弹——继承原版Bullet，支持光束拖尾 + 引导飞行（变化弹）。
    /// 拖尾由ThingDef上的BeamTrailConfig控制，不挂则无拖尾。
    ///
    /// 架构v2：每tick在TickInterval中创建一段BDPTrailSegment(prev→current)，
    /// 由BDPEffectMapComponent统一管理渲染和生命周期。
    /// 投射物销毁后，已创建的线段自然渐隐。
    ///
    /// 引导飞行：通过 GuidedFlightController 组合模式实现折线弹道。
    /// 重写 ImpactSomething()，到达中间锚点时重置飞行参数继续飞行。
    /// </summary>
    public class Bullet_BDP : Bullet
    {
        /// <summary>上一tick的绘制位置，用于创建线段。</summary>
        private Vector3 previousPosition;

        /// <summary>拖尾Material缓存（首次创建时初始化）。</summary>
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
            // 记录初始位置，避免第一tick产生从原点到当前位置的超长线段
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
        /// waypoints 包含所有锚点 + 最终目标（不含起点）。
        /// </summary>
        public void InitGuidedFlight(List<Vector3> waypoints)
        {
            if (waypoints == null || waypoints.Count < 2) return;
            guidedController = new GuidedFlightController(waypoints);
            // 重定向到第一个锚点（waypoints[0]）
            destination = guidedController.CurrentWaypoint;
            ticksToImpact = Mathf.CeilToInt(StartingTicksToImpact);
            if (ticksToImpact < 1) ticksToImpact = 1;
        }

        /// <summary>
        /// 引导飞行核心：到达中间锚点时重置飞行参数，继续飞向下一段。
        /// 到达最终目标时正常 Impact。
        /// 原理：ImpactSomething 在 TickInterval 中 ticksToImpact≤0 时调用，
        /// 此时 CheckForFreeInterceptBetween 已执行完毕（沿途拦截天然生效）。
        /// </summary>
        protected override void ImpactSomething()
        {
            if (guidedController != null && guidedController.IsGuided
                && guidedController.TryAdvanceWaypoint())
            {
                // 当前锚点变为新起点，飞向下一路径点
                origin = destination;
                destination = guidedController.CurrentWaypoint;
                ticksToImpact = Mathf.CeilToInt(StartingTicksToImpact);
                if (ticksToImpact < 1) ticksToImpact = 1;
                return; // 不Impact，继续飞行
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
