using System.Collections.Generic;
using BDP.Core;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 齐射变化弹Verb——继承Verb_BDPVolley，添加多步锚点瞄准和引导飞行。
    ///
    /// 与Verb_BDPVolley的区别：
    ///   · StartGuidedTargeting：由Gizmo右键直接调用，绕过原版targeting流程
    ///   · 重写OnProjectileLaunched：每颗子弹独立计算散布后附加引导路径
    ///   · 重写TryStartCastOn：修正预热朝向为第一个锚点
    ///
    /// 齐射时每颗子弹独立计算锚点散布偏移，形成扇形折线弹幕。
    /// </summary>
    public class Verb_BDPGuidedVolley : Verb_BDPVolley
    {
        /// <summary>锚点原始坐标（未散布）。</summary>
        private List<IntVec3> rawAnchors;

        /// <summary>最终目标。</summary>
        private LocalTargetInfo rawFinalTarget;

        /// <summary>芯片散布半径缓存。</summary>
        private float cachedAnchorSpread;

        /// <summary>是否处于引导齐射模式。</summary>
        private bool guidedVolleyActive;

        /// <summary>瞄准确认时快照的目标地格（不随Thing移动）。</summary>
        private IntVec3 guidedTargetCell;

        /// <summary>
        /// 直接启动多步锚点瞄准（由Command_BDPChipAttack.GizmoOnGUIInt右键调用）。
        /// 绕过原版targeting流程，解决地面瞄准和目标丢弃问题。
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
                (anchors, finalTarget) =>
                {
                    rawAnchors = new List<IntVec3>(anchors);
                    rawFinalTarget = new LocalTargetInfo(finalTarget.Cell); // Cell-only，断开Thing引用
                    cachedAnchorSpread = cfg.anchorSpread;
                    guidedTargetCell = finalTarget.Cell;           // 快照地格
                    guidedVolleyActive = anchors.Count > 0;        // 无锚点=直射模式
                    base.OrderForceTarget(finalTarget);
                });
        }

        public override void OrderForceTarget(LocalTargetInfo target)
        {
            var cfg = GetChipConfig();
            if (cfg != null && cfg.supportsGuided)
            {
                GuidedTargetingHelper.BeginGuidedTargeting(
                    this, CasterPawn, cfg.maxAnchors, verbProps.range,
                    (anchors, finalTarget) =>
                    {
                        // 齐射模式：存储原始锚点，每颗子弹在OnProjectileLaunched中独立散布
                        rawAnchors = new List<IntVec3>(anchors);
                        rawFinalTarget = new LocalTargetInfo(finalTarget.Cell); // Cell-only，断开Thing引用
                        cachedAnchorSpread = cfg.anchorSpread;
                        guidedTargetCell = finalTarget.Cell;           // 快照地格
                        guidedVolleyActive = anchors.Count > 0;        // 无锚点=直射模式
                        base.OrderForceTarget(finalTarget);
                    });
                return;
            }
            guidedVolleyActive = false;
            base.OrderForceTarget(target);
        }

        /// <summary>
        /// v7.0修复：引导齐射模式下LOS检查目标重定向为第一个锚点。
        /// 与Verb_BDPGuided.GetLosCheckTarget对称。
        /// </summary>
        protected override LocalTargetInfo GetLosCheckTarget()
        {
            if (guidedVolleyActive && rawAnchors != null && rawAnchors.Count > 0)
                return new LocalTargetInfo(rawAnchors[0]);
            return base.GetLosCheckTarget();
        }

        /// <summary>
        /// 重写TryStartCastOn：引导齐射模式下用第一个锚点替代最终目标进行LOS检查。
        /// 与Verb_BDPGuided.TryStartCastOn对称。
        /// </summary>
        public override bool TryStartCastOn(LocalTargetInfo castTarg, LocalTargetInfo destTarg,
            bool surpriseAttack = false, bool canHitNonTargetPawns = true,
            bool preventFriendlyFire = false, bool nonInterruptingSelfCast = false)
        {
            LocalTargetInfo actualTarget = castTarg;
            if (guidedVolleyActive && rawAnchors != null && rawAnchors.Count > 0)
                castTarg = new LocalTargetInfo(rawAnchors[0]);

            bool result = base.TryStartCastOn(castTarg, destTarg, surpriseAttack,
                canHitNonTargetPawns, preventFriendlyFire, nonInterruptingSelfCast);

            // currentTarget设为固定Cell → pawn不转身 → 射程不无限
            if (result && guidedVolleyActive)
                currentTarget = new LocalTargetInfo(guidedTargetCell);

            return result;
        }

        /// <summary>
        /// 每颗子弹独立计算散布偏移后附加引导路径。
        /// </summary>
        protected override void OnProjectileLaunched(Projectile proj)
        {
            if (!guidedVolleyActive || rawAnchors == null || rawAnchors.Count == 0)
            {
                guidedVolleyActive = false;
                return;
            }

            // 为这颗子弹独立构建路径点（独立散布），复用Verb_BDPGuided的静态方法
            var waypoints = Verb_BDPGuided.BuildWaypoints(rawAnchors, rawFinalTarget, cachedAnchorSpread);
            if (waypoints.Count >= 2)
            {
                if (proj is Bullet_BDP bdp)
                    bdp.InitGuidedFlight(waypoints);
                else if (proj is Projectile_ExplosiveBDP ebdp)
                    ebdp.InitGuidedFlight(waypoints);
            }
            // 不清除——齐射循环中多次调用
        }
    }
}
