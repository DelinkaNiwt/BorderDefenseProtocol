using System.Collections.Generic;
using BDP.Core;
using UnityEngine;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 齐射变化弹Verb——继承Verb_BDPVolley，添加多步锚点瞄准和引导飞行。
    ///
    /// 与Verb_BDPVolley的区别：
    ///   · 重写OrderForceTarget：启动多步瞄准
    ///   · 重写OnProjectileLaunched：每颗子弹独立计算散布后附加引导路径
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
                        rawFinalTarget = finalTarget;
                        cachedAnchorSpread = cfg.anchorSpread;
                        guidedVolleyActive = true;
                        base.OrderForceTarget(finalTarget);
                    });
                return;
            }
            base.OrderForceTarget(target);
        }

        /// <summary>
        /// 每颗子弹独立计算散布偏移后附加引导路径。
        /// 齐射结束后（最后一颗子弹发射后）清除状态。
        /// </summary>
        protected override void OnProjectileLaunched(Projectile proj)
        {
            if (!guidedVolleyActive || rawAnchors == null || rawAnchors.Count == 0)
            {
                guidedVolleyActive = false;
                return;
            }

            // 为这颗子弹独立构建路径点（独立散布）
            var waypoints = BuildWaypointsWithSpread(rawAnchors, rawFinalTarget, cachedAnchorSpread);
            if (waypoints.Count >= 2)
            {
                if (proj is Bullet_BDP bdp)
                    bdp.InitGuidedFlight(waypoints);
                else if (proj is Projectile_ExplosiveBDP ebdp)
                    ebdp.InitGuidedFlight(waypoints);
            }
            // 注意：不在此处清除guidedVolleyActive，因为齐射循环中会多次调用
            // 清除由TryCastShot结束后处理（齐射循环外）
        }

        /// <summary>从芯片配置读取WeaponChipConfig（三级回退）。</summary>
        private WeaponChipConfig GetChipConfig()
        {
            var pawn = CasterPawn;
            if (pawn == null) return null;
            var triggerComp = pawn.equipment?.Primary?.TryGetComp<CompTriggerBody>();
            if (triggerComp == null) return null;

            SlotSide? side = DualVerbCompositor.ParseSideLabel(verbProps?.label);
            if (side.HasValue)
            {
                var sideSlot = triggerComp.GetActiveSlot(side.Value);
                var cfg = sideSlot?.loadedChip?.def?.GetModExtension<WeaponChipConfig>();
                if (cfg != null) return cfg;
            }
            var slot = triggerComp.ActivatingSlot;
            if (slot?.loadedChip != null)
            {
                var cfg = slot.loadedChip.def.GetModExtension<WeaponChipConfig>();
                if (cfg != null) return cfg;
            }
            foreach (var activeSlot in triggerComp.AllActiveSlots())
            {
                var cfg = activeSlot.loadedChip?.def?.GetModExtension<WeaponChipConfig>();
                if (cfg != null) return cfg;
            }
            return null;
        }

        /// <summary>构建路径点（每颗子弹独立随机散布）。</summary>
        private static List<Vector3> BuildWaypointsWithSpread(
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

            waypoints.Add(finalTarget.Cell.ToVector3Shifted());
            return waypoints;
        }
    }
}
