using System.Collections.Generic;
using BDP.Core;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 齐射变化弹Verb——继承Verb_BDPVolley，添加多步锚点瞄准和引导飞行。
    /// Fix-2：引导状态委托给GuidedVerbState组合类。
    ///
    /// 齐射时每颗子弹独立计算锚点散布偏移，形成扇形折线弹幕。
    /// </summary>
    public class Verb_BDPGuidedVolley : Verb_BDPVolley
    {
        /// <summary>引导弹共享状态（Fix-2：组合模式替代重复字段）。</summary>
        private readonly GuidedVerbState gs = new GuidedVerbState();

        /// <summary>直接启动多步锚点瞄准。</summary>
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

        /// <summary>引导齐射模式下LOS检查目标重定向为第一个锚点。</summary>
        protected override LocalTargetInfo GetLosCheckTarget()
            => gs.GetLosCheckTarget(base.GetLosCheckTarget());

        /// <summary>引导齐射模式下用第一个锚点替代最终目标进行LOS检查。</summary>
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

        /// <summary>每颗子弹独立计算散布偏移后附加引导路径。</summary>
        protected override void OnProjectileLaunched(Projectile proj)
        {
            if (!gs.GuidedActive) return;
            gs.AttachGuidedFlight(proj);
        }
    }
}
