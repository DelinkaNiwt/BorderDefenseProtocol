using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 引导飞行模块——折线弹道逻辑。
    /// Priority=10（路径修改，优先执行）。
    ///
    /// v5管线接口：
    ///   IBDPArrivalPolicy（到达锚点时重定向到下一路径点）
    ///   IBDPFlightIntentProvider（首tick重定向到第一路径点，一次性）
    ///
    /// 内部持有GuidedFlightController，管理多段折线飞行。
    /// Verb层通过ApplyWaypoints()注入锚点和最终目标。
    ///
    /// v5变更：
    ///   · 不再直接写host.IsOnFinalSegment
    ///   · 不再调用host.RedirectFlightGuided
    ///   · 通过ctx.Continue + ctx.NextDestination + ctx.RequestPhaseChange表达意图
    ///   · 调用host.InitGuidedFlight()初始化引导飞行
    /// </summary>
    public class GuidedModule : IBDPProjectileModule, IBDPArrivalPolicy, IBDPFlightIntentProvider
    {
        /// <summary>引导飞行控制器（null=尚未初始化或普通弹道）。</summary>
        private GuidedFlightController controller;

        /// <summary>首次飞行意图标记——ApplyWaypoints后置true，首tick ProvideIntent消费后置false。</summary>
        private bool needsInitialRedirect;

        public int Priority => 10;

        /// <summary>无参构造——Scribe反序列化需要。</summary>
        public GuidedModule() { }

        public GuidedModule(BDPGuidedConfig config)
        {
            // 标记类，无需存储配置
        }

        public void OnSpawn(Bullet_BDP host) { }

        /// <summary>
        /// IBDPArrivalPolicy实现——到达中间锚点时拦截Impact，重定向飞行到下一路径点。
        /// 设置ctx.Continue=true表示继续飞行（宿主跳过Impact）。
        /// </summary>
        public void DecideArrival(Bullet_BDP host, ref ArrivalContextV5 ctx)
        {
            if (controller == null || !controller.IsGuided) return;
            if (!controller.TryAdvanceWaypoint()) return;

            if (!controller.IsGuided)
            {
                // ★ 进入最终段：用目标实时位置替代预计算路径点。
                // 原因：预计算路径点基于开枪瞬间的Cell坐标，
                //       飞行途中目标pawn可能已移动，导致初始方向偏离实际目标。
                //       仅当FinalTarget持有有效Thing时生效，Cell目标回退预计算值。
                Vector3 finalDest = (host.FinalTarget.Thing != null
                                     && host.FinalTarget.Thing.Spawned)
                    ? host.FinalTarget.Thing.DrawPos
                    : controller.CurrentWaypoint;
                ctx.Continue = true;
                ctx.NextDestination = finalDest;
                ctx.RequestPhaseChange = FlightPhase.FinalApproach;

                if (TrackingDiag.Enabled)
                {
                    IntVec3 destCell = finalDest.ToIntVec3();
                    bool los = host.Map != null
                        && GenSight.LineOfSight(host.Position, destCell, host.Map);
                    Log.Message($"[BDP-Guided] →最终段 dest={finalDest:F2} LOS={los}");
                }
            }
            else
            {
                // 中间锚点：使用预计算路径点
                ctx.Continue = true;
                ctx.NextDestination = controller.CurrentWaypoint;
                ctx.RequestPhaseChange = FlightPhase.GuidedLeg;

                if (TrackingDiag.Enabled)
                {
                    IntVec3 wpCell = controller.CurrentWaypoint.ToIntVec3();
                    bool los = host.Map != null
                        && GenSight.LineOfSight(host.Position, wpCell, host.Map);
                    Log.Message($"[BDP-Guided] →锚点 dest={controller.CurrentWaypoint:F2} LOS={los}");
                }
            }
        }

        /// <summary>
        /// IBDPFlightIntentProvider实现——仅在ApplyWaypoints后的首tick提供一次飞行意图，
        /// 将子弹重定向到第一个路径点。之后每tick立即返回（无意图）。
        ///
        /// 原因：v5管线中Launch后的首次飞行方向由vanilla设定（指向autoRouteLosCell），
        ///       齐射交替分配左/右路径后，"另一侧"子弹的初始方向与实际路径不匹配。
        ///       通过管线Stage2在首tick修正，子弹从未朝错误方向移动。
        /// </summary>
        public void ProvideIntent(Bullet_BDP host, ref FlightIntentContext ctx)
        {
            if (!needsInitialRedirect) return;
            needsInitialRedirect = false;

            if (controller == null || !controller.IsGuided) return;

            ctx.Intent = new FlightIntent
            {
                TargetPosition = controller.CurrentWaypoint,
                TrackingActivated = false
            };
        }

        /// <summary>
        /// 由Verb层调用——设置锚点和最终目标，构建路径点并初始化引导飞行。
        /// v5变更：调用host.InitGuidedFlight()设置Phase和同步目标，
        ///         不再直接写host.IsOnFinalSegment。
        /// v5命名修正：SetWaypoints → ApplyWaypoints。
        /// </summary>
        internal void ApplyWaypoints(Bullet_BDP host,
            List<IntVec3> anchors, LocalTargetInfo finalTarget, float anchorSpread)
        {
            if (anchors == null || anchors.Count == 0) return;

            var waypoints = WaypointBuilder.BuildWaypoints(anchors, finalTarget, anchorSpread);
            if (waypoints.Count < 2) return;

            controller = new GuidedFlightController(waypoints);

            // v5：通过宿主API初始化引导飞行（设置Phase=GuidedLeg + 同步目标）
            host.InitGuidedFlight(finalTarget);
            needsInitialRedirect = true;  // 标记：下一tick通过管线Stage2重定向到首路径点

        }

        public void ExposeData()
        {
            Scribe_Deep.Look(ref controller, "guidedController");
        }
    }
}
