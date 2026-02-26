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
    /// v7.0变化弹适配。Fix-2：引导状态委托给GuidedVerbState组合类。
    ///
    /// 操作流程：
    ///   点击Gizmo → StartGuidedTargeting → Shift+左键放置锚点 → 左键确认最终目标 → 发射
    ///   芯片配置 supportsGuided=true 时激活，否则回退到普通射击。
    /// </summary>
    public class Verb_BDPGuided : Verb_BDPShoot
    {
        /// <summary>引导弹共享状态（Fix-2：组合模式替代重复字段）。</summary>
        private readonly GuidedVerbState gs = new GuidedVerbState();

        /// <summary>
        /// 直接启动多步锚点瞄准（由Command_BDPChipAttack.GizmoOnGUIInt调用）。
        /// </summary>
        public void StartGuidedTargeting()
        {
            var cfg = GetChipConfig();
            if (cfg == null || !cfg.supportsGuided)
            {
                Find.Targeter.BeginTargeting(this);
                return;
            }

            GuidedTargetingHelper.BeginGuidedTargeting(
                this, CasterPawn, cfg.maxAnchors, verbProps.range,
                (anchors, finalTarget) =>
                {
                    gs.StoreTargetingResult(anchors, finalTarget, cfg.anchorSpread);
                    base.OrderForceTarget(finalTarget);
                });
        }

        /// <summary>OrderForceTarget保留作为回退路径。</summary>
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            var cfg = GetChipConfig();
            if (cfg != null && cfg.supportsGuided)
            {
                GuidedTargetingHelper.BeginGuidedTargeting(
                    this, CasterPawn, cfg.maxAnchors, verbProps.range,
                    (anchors, finalTarget) =>
                    {
                        gs.StoreTargetingResult(anchors, finalTarget, cfg.anchorSpread);
                        base.OrderForceTarget(finalTarget);
                    });
                return;
            }
            gs.GuidedActive = false;
            base.OrderForceTarget(target);
        }

        /// <summary>引导模式下LOS检查目标重定向为第一个锚点。</summary>
        protected override LocalTargetInfo GetLosCheckTarget()
            => gs.GetLosCheckTarget(base.GetLosCheckTarget());

        /// <summary>引导模式下用第一个锚点替代最终目标进行LOS检查。</summary>
        public override bool TryStartCastOn(LocalTargetInfo castTarg, LocalTargetInfo destTarg,
            bool surpriseAttack = false, bool canHitNonTargetPawns = true,
            bool preventFriendlyFire = false, bool nonInterruptingSelfCast = false)
        {
            gs.InterceptCastTarget(ref castTarg);

            bool result = base.TryStartCastOn(castTarg, destTarg, surpriseAttack,
                canHitNonTargetPawns, preventFriendlyFire, nonInterruptingSelfCast);

            if (result)
                gs.PostCastOn(ref currentTarget);

            return result;
        }

        /// <summary>每颗子弹独立构建路径点（独立散布偏移）。</summary>
        protected override void OnProjectileLaunched(Projectile proj)
        {
            if (!gs.GuidedActive) return;
            gs.AttachGuidedFlight(proj);
        }

        /// <summary>
        /// 构建路径点列表：锚点坐标 + 最终目标坐标，应用递增散布偏移。
        /// 散布公式：actualAnchor[i] = anchor[i] + Random.insideUnitCircle * spread * (i+1)/total
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
    }
}
