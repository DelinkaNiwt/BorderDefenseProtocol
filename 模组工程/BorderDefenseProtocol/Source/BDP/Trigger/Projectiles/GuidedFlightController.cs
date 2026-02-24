using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 引导飞行控制器（组合模式）——管理变化弹的多段折线飞行。
    ///
    /// 使用方式：
    ///   1. Verb发射弹道后，调用 projectile.InitGuidedFlight(waypoints)
    ///   2. 弹道子类重写 ImpactSomething()，到达锚点时调用 TryAdvanceWaypoint()
    ///   3. 返回true → 重置 origin/destination/ticksToImpact，继续飞行
    ///   4. 返回false → 已到最终目标，正常 Impact
    ///
    /// 沿途拦截由原版 TickInterval.CheckForFreeInterceptBetween 天然处理，
    /// 被拦截时 Impact 正常触发，剩余路径自动取消。
    /// </summary>
    public class GuidedFlightController : IExposable
    {
        /// <summary>所有路径点（不含起点，含最终目标）。</summary>
        private List<Vector3> waypoints;

        /// <summary>当前目标路径点索引（0=第一个锚点或最终目标）。</summary>
        private int currentIndex;

        /// <summary>是否处于引导模式（有路径点且未到达最终目标）。</summary>
        public bool IsGuided => waypoints != null && waypoints.Count > 0
                                && currentIndex < waypoints.Count - 1;

        /// <summary>当前目标路径点。</summary>
        public Vector3 CurrentWaypoint => waypoints[currentIndex];

        /// <summary>无参构造——Scribe反序列化需要。</summary>
        public GuidedFlightController() { }

        public GuidedFlightController(List<Vector3> waypoints)
        {
            this.waypoints = waypoints;
            currentIndex = 0;
        }

        /// <summary>
        /// 到达当前锚点时调用。推进到下一路径点。
        /// 返回true=已推进（调用者应重置飞行参数到 CurrentWaypoint），
        /// 返回false=已在最终目标（调用者应正常Impact）。
        ///
        /// 流程示例（waypoints=[A, B, Final]）：
        ///   到达A → advance → index=1 → true（飞向B）
        ///   到达B → advance → index=2 → true（飞向Final）
        ///   到达Final → IsGuided=false → 不调用本方法 → Impact
        /// </summary>
        public bool TryAdvanceWaypoint()
        {
            if (waypoints == null || currentIndex >= waypoints.Count - 1)
                return false; // 已在最终目标或无路径
            currentIndex++;
            return true; // 已推进，调用者重定向到 CurrentWaypoint
        }

        public void ExposeData()
        {
            Scribe_Collections.Look(ref waypoints, "guidedWaypoints", LookMode.Value);
            Scribe_Values.Look(ref currentIndex, "guidedIndex", 0);
        }
    }
}
