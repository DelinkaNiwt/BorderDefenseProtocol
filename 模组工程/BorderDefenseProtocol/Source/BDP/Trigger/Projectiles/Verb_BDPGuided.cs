using System.Collections.Generic;
using BDP.Core;
using UnityEngine;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 单发变化弹Verb——继承Verb_BDPShoot，添加多步锚点瞄准和引导飞行。
    ///
    /// 与Verb_BDPShoot的区别：
    ///   · 重写OrderForceTarget：启动多步瞄准（Shift+点击放置锚点）
    ///   · 重写OnProjectileLaunched：将路径点附加到弹道，实现折线飞行
    ///
    /// 操作流程：
    ///   点击Gizmo → 进入瞄准 → Shift+左键放置锚点 → 左键确认最终目标 → 发射
    ///   芯片配置 supportsGuided=true 时激活，否则回退到普通射击。
    /// </summary>
    public class Verb_BDPGuided : Verb_BDPShoot
    {
        /// <summary>待发射的路径点列表（锚点+最终目标的Vector3坐标）。</summary>
        private List<Vector3> guidedWaypoints;

        /// <summary>
        /// 重写OrderForceTarget：芯片支持变化弹时启动多步瞄准，
        /// 否则回退到基类的直接射击。
        /// </summary>
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            var cfg = GetChipConfig();
            if (cfg != null && cfg.supportsGuided)
            {
                // 启动多步锚点瞄准
                GuidedTargetingHelper.BeginGuidedTargeting(
                    this, CasterPawn, cfg.maxAnchors, verbProps.range,
                    (anchors, finalTarget) =>
                    {
                        // 构建路径点：锚点 + 最终目标，应用散布偏移
                        guidedWaypoints = BuildWaypoints(anchors, finalTarget, cfg.anchorSpread);
                        // 调用基类创建Job发射
                        base.OrderForceTarget(finalTarget);
                    });
                return;
            }
            base.OrderForceTarget(target);
        }

        /// <summary>
        /// 弹道发射后回调：将引导路径附加到弹道实例。
        /// 路径点数≥2时（至少1个锚点+最终目标）才启用引导。
        /// </summary>
        protected override void OnProjectileLaunched(Projectile proj)
        {
            if (guidedWaypoints != null && guidedWaypoints.Count >= 2)
            {
                if (proj is Bullet_BDP bdp)
                    bdp.InitGuidedFlight(guidedWaypoints);
                else if (proj is Projectile_ExplosiveBDP ebdp)
                    ebdp.InitGuidedFlight(guidedWaypoints);
            }
            // 每颗子弹独立路径，单发模式发射后清除
            guidedWaypoints = null;
        }

        /// <summary>
        /// 从芯片配置读取WeaponChipConfig。
        /// 复用Verb_BDPShoot的三级回退策略（侧别label → ActivatingSlot → 遍历）。
        /// </summary>
        private WeaponChipConfig GetChipConfig()
        {
            var pawn = CasterPawn;
            if (pawn == null) return null;
            var triggerComp = pawn.equipment?.Primary?.TryGetComp<CompTriggerBody>();
            if (triggerComp == null) return null;

            // 优先通过侧别label精确定位
            SlotSide? side = DualVerbCompositor.ParseSideLabel(verbProps?.label);
            if (side.HasValue)
            {
                var sideSlot = triggerComp.GetActiveSlot(side.Value);
                var cfg = sideSlot?.loadedChip?.def?.GetModExtension<WeaponChipConfig>();
                if (cfg != null) return cfg;
            }
            // 回退：ActivatingSlot
            var slot = triggerComp.ActivatingSlot;
            if (slot?.loadedChip != null)
            {
                var cfg = slot.loadedChip.def.GetModExtension<WeaponChipConfig>();
                if (cfg != null) return cfg;
            }
            // 最终回退：遍历
            foreach (var activeSlot in triggerComp.AllActiveSlots())
            {
                var cfg = activeSlot.loadedChip?.def?.GetModExtension<WeaponChipConfig>();
                if (cfg != null) return cfg;
            }
            return null;
        }

        /// <summary>
        /// 构建路径点列表：锚点坐标 + 最终目标坐标，应用递增散布偏移。
        /// 散布公式：actualAnchor[i] = anchor[i] + Random.insideUnitCircle * spread * (i+1)/total
        /// </summary>
        private static List<Vector3> BuildWaypoints(
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

            // 最终目标（原版Launch已有0.3f随机偏移，此处不额外散布）
            waypoints.Add(finalTarget.Cell.ToVector3Shifted());
            return waypoints;
        }
    }
}
