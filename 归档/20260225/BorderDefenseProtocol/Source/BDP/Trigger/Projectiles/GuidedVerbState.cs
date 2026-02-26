using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 引导弹Verb的共享状态和逻辑（组合模式，Fix-2）。
    /// 管理锚点数据、LOS检查重定向、TryStartCastOn拦截、弹道附加。
    /// 各Verb类持有此实例，委托引导相关操作。
    ///
    /// 使用方：
    ///   · Verb_BDPGuided（单发引导）
    ///   · Verb_BDPGuidedVolley（单侧齐射引导）
    ///   · Verb_BDPDualRanged（双侧burst引导）
    ///   · Verb_BDPDualVolley（双侧齐射引导）
    /// </summary>
    public class GuidedVerbState
    {
        // ── 共享状态 ──
        /// <summary>锚点原始坐标（未散布）。</summary>
        public List<IntVec3> RawAnchors;
        /// <summary>最终目标。</summary>
        public LocalTargetInfo RawFinalTarget;
        /// <summary>芯片散布半径缓存。</summary>
        public float CachedAnchorSpread;
        /// <summary>是否处于引导模式。</summary>
        public bool GuidedActive;
        /// <summary>瞄准确认时快照的目标地格（不随Thing移动）。</summary>
        public IntVec3 GuidedTargetCell;

        // ── 双侧专用（单侧Verb不使用） ──
        /// <summary>原始Thing目标引用（供非变化弹侧跟踪）。</summary>
        public LocalTargetInfo SavedThingTarget;
        /// <summary>左侧是否为变化弹。</summary>
        public bool LeftIsGuided;
        /// <summary>右侧是否为变化弹。</summary>
        public bool RightIsGuided;
        /// <summary>当前发射的子弹是否属于变化弹侧。</summary>
        public bool CurrentShotIsGuided;

        /// <summary>存储锚点瞄准结果。</summary>
        public void StoreTargetingResult(
            List<IntVec3> anchors, LocalTargetInfo finalTarget, float spread)
        {
            RawAnchors = new List<IntVec3>(anchors);
            RawFinalTarget = new LocalTargetInfo(finalTarget.Cell);
            CachedAnchorSpread = spread;
            GuidedTargetCell = finalTarget.Cell;
            GuidedActive = anchors.Count > 0;
        }

        /// <summary>获取LOS检查目标（单侧模式：引导时返回第一锚点）。</summary>
        public LocalTargetInfo GetLosCheckTarget(LocalTargetInfo defaultTarget)
        {
            if (GuidedActive && RawAnchors?.Count > 0)
                return new LocalTargetInfo(RawAnchors[0]);
            return defaultTarget;
        }

        /// <summary>获取LOS检查目标（双侧模式：感知CurrentShotIsGuided）。</summary>
        public LocalTargetInfo GetDualLosCheckTarget(LocalTargetInfo defaultTarget)
        {
            if (GuidedActive && CurrentShotIsGuided && RawAnchors?.Count > 0)
                return new LocalTargetInfo(RawAnchors[0]);
            return defaultTarget;
        }

        /// <summary>
        /// TryStartCastOn前处理（单侧模式）：替换castTarg为第一锚点。
        /// 返回实际最终目标（调用方需保存）。
        /// </summary>
        public LocalTargetInfo InterceptCastTarget(ref LocalTargetInfo castTarg)
        {
            LocalTargetInfo actualTarget = castTarg;
            if (GuidedActive && RawAnchors != null && RawAnchors.Count > 0)
                castTarg = new LocalTargetInfo(RawAnchors[0]);
            return actualTarget;
        }

        /// <summary>
        /// TryStartCastOn前处理（双侧模式）：根据LOS选择面朝方向。
        /// 返回实际最终目标（调用方需保存）。
        /// </summary>
        public LocalTargetInfo InterceptDualCastTarget(
            ref LocalTargetInfo castTarg, IntVec3 casterPos, Map map)
        {
            LocalTargetInfo actualTarget = castTarg;
            if (GuidedActive && RawAnchors != null && RawAnchors.Count > 0)
            {
                bool canSeeTarget = GenSight.LineOfSight(casterPos, actualTarget.Cell, map);
                castTarg = canSeeTarget ? new LocalTargetInfo(actualTarget.Cell)
                                        : new LocalTargetInfo(RawAnchors[0]);
            }
            return actualTarget;
        }

        /// <summary>TryStartCastOn后处理（单侧模式）：锁定currentTarget为Cell。</summary>
        public void PostCastOn(ref LocalTargetInfo currentTarget)
        {
            if (GuidedActive)
                currentTarget = new LocalTargetInfo(GuidedTargetCell);
        }

        /// <summary>TryStartCastOn后处理（双侧模式）：保存Thing并锁定Cell。</summary>
        public void PostDualCastOn(
            ref LocalTargetInfo currentTarget, LocalTargetInfo actualTarget)
        {
            if (GuidedActive)
            {
                SavedThingTarget = actualTarget;
                currentTarget = new LocalTargetInfo(GuidedTargetCell);
            }
        }

        /// <summary>为弹道附加引导路径（通用）。</summary>
        public void AttachGuidedFlight(Projectile proj)
        {
            if (!GuidedActive || RawAnchors == null || RawAnchors.Count == 0)
                return;
            var waypoints = Verb_BDPGuided.BuildWaypoints(
                RawAnchors, RawFinalTarget, CachedAnchorSpread);
            if (waypoints.Count >= 2)
            {
                if (proj is Bullet_BDP bdp) bdp.InitGuidedFlight(waypoints);
                else if (proj is Projectile_ExplosiveBDP ebdp) ebdp.InitGuidedFlight(waypoints);
            }
        }

        /// <summary>为弹道附加引导路径（仅当CurrentShotIsGuided时）。</summary>
        public void AttachGuidedFlightIfActive(Projectile proj)
        {
            if (!GuidedActive || !CurrentShotIsGuided)
                return;
            AttachGuidedFlight(proj);
        }

        /// <summary>重置所有状态。</summary>
        public void Reset()
        {
            RawAnchors = null;
            RawFinalTarget = default;
            CachedAnchorSpread = 0f;
            GuidedActive = false;
            GuidedTargetCell = default;
            SavedThingTarget = default;
            LeftIsGuided = false;
            RightIsGuided = false;
            CurrentShotIsGuided = false;
        }
    }
}