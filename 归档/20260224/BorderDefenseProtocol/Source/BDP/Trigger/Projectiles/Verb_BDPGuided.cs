using System.Collections.Generic;
using BDP.Core;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 单发变化弹Verb——继承Verb_BDPShoot，添加多步锚点瞄准和引导飞行。
    ///
    /// 与Verb_BDPShoot的区别：
    ///   · StartGuidedTargeting：由Gizmo直接调用，绕过原版targeting流程
    ///   · 重写OnProjectileLaunched：将路径点附加到弹道，实现折线飞行
    ///   · 重写TryStartCastOn：修正预热朝向为第一个锚点
    ///
    /// 操作流程：
    ///   点击Gizmo → StartGuidedTargeting → Shift+左键放置锚点 → 左键确认最终目标 → 发射
    ///   芯片配置 supportsGuided=true 时激活，否则回退到普通射击。
    /// </summary>
    public class Verb_BDPGuided : Verb_BDPShoot
    {
        /// <summary>锚点原始坐标（未散布）。与Verb_BDPGuidedVolley对齐的raw anchor模式。</summary>
        private List<IntVec3> rawAnchors;

        /// <summary>最终目标。</summary>
        private LocalTargetInfo rawFinalTarget;

        /// <summary>芯片散布半径缓存。</summary>
        private float cachedAnchorSpread;

        /// <summary>是否处于引导模式。</summary>
        private bool guidedActive;

        /// <summary>瞄准确认时快照的目标地格（不随Thing移动）。</summary>
        private IntVec3 guidedTargetCell;

        /// <summary>
        /// 直接启动多步锚点瞄准（由Command_BDPChipAttack.GizmoOnGUIInt调用）。
        /// 绕过原版targeting流程，解决两个问题：
        ///   1. 原版verb.targetParams不允许地面瞄准
        ///   2. OrderForceTarget会丢弃第一个目标
        /// </summary>
        public void StartGuidedTargeting()
        {
            var cfg = GetChipConfig();
            if (cfg == null || !cfg.supportsGuided)
            {
                // 回退：不支持引导时走原版targeting
                Find.Targeter.BeginTargeting(this);
                return;
            }

            GuidedTargetingHelper.BeginGuidedTargeting(
                this, CasterPawn, cfg.maxAnchors, verbProps.range,
                (anchors, finalTarget) => OnGuidedTargetingComplete(anchors, finalTarget, cfg.anchorSpread));
        }

        /// <summary>
        /// OrderForceTarget保留作为回退路径。
        /// 正常流程由StartGuidedTargeting启动，此方法仅在非引导芯片时使用。
        /// </summary>
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            var cfg = GetChipConfig();
            if (cfg != null && cfg.supportsGuided)
            {
                // 引导芯片：启动多步瞄准（兼容非Gizmo调用路径）
                GuidedTargetingHelper.BeginGuidedTargeting(
                    this, CasterPawn, cfg.maxAnchors, verbProps.range,
                    (anchors, finalTarget) => OnGuidedTargetingComplete(anchors, finalTarget, cfg.anchorSpread));
                return;
            }
            guidedActive = false;
            base.OrderForceTarget(target);
        }

        /// <summary>
        /// 锚点瞄准完成回调：存储锚点数据并调用基类发射。
        /// 由StartGuidedTargeting和OrderForceTarget共用。
        /// </summary>
        private void OnGuidedTargetingComplete(
            List<IntVec3> anchors, LocalTargetInfo finalTarget, float anchorSpread)
        {
            rawAnchors = new List<IntVec3>(anchors);
            rawFinalTarget = new LocalTargetInfo(finalTarget.Cell); // Cell-only，断开Thing引用
            cachedAnchorSpread = anchorSpread;
            guidedTargetCell = finalTarget.Cell;    // 快照地格
            guidedActive = anchors.Count > 0;       // 无锚点=直射模式
            base.OrderForceTarget(finalTarget);
        }

        /// <summary>
        /// v7.0修复：引导模式下LOS检查目标重定向为第一个锚点。
        /// 原因：TryCastShotCore检查caster→currentTarget的直线LOS，
        /// 但引导弹经由锚点折线飞行，只需caster→第一锚点有LOS。
        /// 后续段的LOS已在GuidedTargetingHelper瞄准阶段校验。
        /// </summary>
        protected override LocalTargetInfo GetLosCheckTarget()
        {
            if (guidedActive && rawAnchors != null && rawAnchors.Count > 0)
                return new LocalTargetInfo(rawAnchors[0]);
            return base.GetLosCheckTarget();
        }

        /// <summary>
        /// 重写TryStartCastOn：引导模式下用第一个锚点替代最终目标进行LOS检查。
        /// 原因：Verb.TryStartCastOn内部有CanHitTarget和TryFindShootLineFromTo两道LOS检查，
        ///   都检查caster→castTarg的直线LOS。引导弹经由锚点折线飞行，只需到第一锚点有LOS。
        /// 副作用：Stance_Warmup(focusTarg=firstAnchor)使pawn自然面向第一锚点——正确行为。
        /// </summary>
        public override bool TryStartCastOn(LocalTargetInfo castTarg, LocalTargetInfo destTarg,
            bool surpriseAttack = false, bool canHitNonTargetPawns = true,
            bool preventFriendlyFire = false, bool nonInterruptingSelfCast = false)
        {
            // 保存实际最终目标，用第一锚点骗过base的LOS检查
            LocalTargetInfo actualTarget = castTarg;
            if (guidedActive && rawAnchors != null && rawAnchors.Count > 0)
                castTarg = new LocalTargetInfo(rawAnchors[0]);

            bool result = base.TryStartCastOn(castTarg, destTarg, surpriseAttack,
                canHitNonTargetPawns, preventFriendlyFire, nonInterruptingSelfCast);

            // currentTarget设为固定Cell → pawn不转身 → 射程不无限 → 弹道飞向快照地格
            if (result && guidedActive)
                currentTarget = new LocalTargetInfo(guidedTargetCell);

            return result;
        }

        /// <summary>
        /// 不清除guidedActive——burst连射下多次调用，下次瞄准时会覆盖。
        /// </summary>
        protected override void OnProjectileLaunched(Projectile proj)
        {
            if (!guidedActive || rawAnchors == null || rawAnchors.Count == 0)
            {
                guidedActive = false;
                return;
            }

            // 每颗子弹独立构建路径点（独立散布偏移）
            var waypoints = BuildWaypoints(rawAnchors, rawFinalTarget, cachedAnchorSpread);
            if (waypoints.Count >= 2)
            {
                if (proj is Bullet_BDP bdp)
                    bdp.InitGuidedFlight(waypoints);
                else if (proj is Projectile_ExplosiveBDP ebdp)
                    ebdp.InitGuidedFlight(waypoints);
            }
            // 不清除——burst连射下多次调用
        }

        /// <summary>
        /// 构建路径点列表：锚点坐标 + 最终目标坐标，应用递增散布偏移。
        /// 散布公式：actualAnchor[i] = anchor[i] + Random.insideUnitCircle * spread * (i+1)/total
        /// </summary>
        internal static List<Vector3> BuildWaypoints(
            List<IntVec3> anchors, LocalTargetInfo finalTarget, float anchorSpread)
        {
            var waypoints = new List<Vector3>();
            int totalSegments = anchors.Count + 1; // 锚点数 + 最终目标

            // 锚点（应用递增散布）
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

            // 最终目标：散布限制在格子内（±0.45），确保IntVec3落在目标格子上
            // 中间锚点保持完整散布提供视觉多样性，终点精度保证命中判定
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
    }
}
