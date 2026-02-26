using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 引导飞行模块——折线弹道逻辑。
    /// Priority=10（路径修改，优先执行）。
    ///
    /// 管线接口：IBDPArrivalHandler（到达中间锚点时重定向飞行）。
    ///
    /// 内部持有GuidedFlightController，管理多段折线飞行。
    /// Verb层通过SetWaypoints()注入锚点和最终目标。
    ///
    /// 路径点构建逻辑（原Verb_BDPGuided.BuildWaypoints）已迁移到本模块内部。
    /// </summary>
    public class GuidedModule : IBDPProjectileModule, IBDPArrivalHandler
    {
        /// <summary>引导飞行控制器（null=尚未初始化或普通弹道）。</summary>
        private GuidedFlightController controller;

        public int Priority => 10;

        /// <summary>无参构造——Scribe反序列化需要。</summary>
        public GuidedModule() { }

        public GuidedModule(BDPGuidedConfig config)
        {
            // 标记类，无需存储配置
        }

        public void OnSpawn(Bullet_BDP host) { }

        /// <summary>
        /// IBDPArrivalHandler实现——到达中间锚点时拦截Impact，重定向飞行到下一路径点。
        /// 设置ctx.Continue=true表示继续飞行（宿主跳过Impact）。
        /// </summary>
        public void HandleArrival(Bullet_BDP host, ref ArrivalContext ctx)
        {
            if (controller != null && controller.IsGuided
                && controller.TryAdvanceWaypoint())
            {
                // 当前位置作为新起点，飞向下一路径点
                host.RedirectFlight(host.DrawPos, controller.CurrentWaypoint);
                ctx.Continue = true;
            }
        }

        /// <summary>
        /// 由Verb层调用——设置锚点和最终目标，构建路径点并初始化引导飞行。
        /// 内部调用BuildWaypoints()构建路径点列表，然后初始化controller。
        /// </summary>
        public void SetWaypoints(Bullet_BDP host,
            List<IntVec3> anchors, LocalTargetInfo finalTarget, float anchorSpread)
        {
            if (anchors == null || anchors.Count == 0) return;

            var waypoints = BuildWaypoints(anchors, finalTarget, anchorSpread);
            if (waypoints.Count < 2) return;

            controller = new GuidedFlightController(waypoints);
            // 重定向到第一个路径点
            host.RedirectFlight(host.DrawPos, controller.CurrentWaypoint);
        }

        /// <summary>
        /// 构建路径点列表：锚点坐标 + 最终目标坐标，应用递增散布偏移。
        /// 散布公式：actualAnchor[i] = anchor[i] + Random.insideUnitCircle * spread * (i+1)/total
        /// （从Verb_BDPGuided.BuildWaypoints迁移）
        /// </summary>
        internal static List<Vector3> BuildWaypoints(
            List<IntVec3> anchors, LocalTargetInfo finalTarget, float anchorSpread)
        {
            var waypoints = new List<Vector3>();
            int totalSegments = anchors.Count + 1;

            for (int i = 0; i < anchors.Count; i++)
            {
                Vector3 basePos = anchors[i].ToVector3Shifted();
                if (anchorSpread > 0f)
                {
                    float factor = (float)(i + 1) / totalSegments;
                    Vector2 offset = Random.insideUnitCircle * anchorSpread * factor;
                    basePos += new Vector3(offset.x, 0f, offset.y);
                }
                waypoints.Add(basePos);
            }

            Vector3 finalPos = finalTarget.Cell.ToVector3Shifted();
            if (anchorSpread > 0f)
            {
                float clampedSpread = Mathf.Min(anchorSpread, 0.45f);
                Vector2 finalOffset = Random.insideUnitCircle * clampedSpread;
                finalPos += new Vector3(finalOffset.x, 0f, finalOffset.y);
            }
            waypoints.Add(finalPos);
            return waypoints;
        }

        public void ExposeData()
        {
            Scribe_Deep.Look(ref controller, "guidedController");
        }
    }
}
