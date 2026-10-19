using BDP.Core.Abilities;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Content.GroundDash
{
    /// <summary>
    /// 中性地面冲刺能力 Verb。
    ///
    /// 只负责单段目标选择、射程和地图几何预览；不包含蚱蜢多段路径。
    /// </summary>
    public class Verb_CastAbilityGroundDash : BdpVerb_CastAbility
    {
        /// <summary>缓存当前能力的最大射程。</summary>
        private float cachedEffectiveRange = -1f;

        /// <summary>地面冲刺不需要施法抖动，避免与位移视觉叠加。</summary>
        protected override void TriggerCastJitter() { }

        /// <summary>当前能力的最大射程。</summary>
        public override float EffectiveRange
        {
            get
            {
                if (cachedEffectiveRange < 0f)
                {
                    cachedEffectiveRange = verbProps != null ? verbProps.range : 0f;
                }
                return cachedEffectiveRange;
            }
        }

        /// <summary>只有完整可达的地图目标格才允许确认施法。</summary>
        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            if (caster == null || caster.Map == null || !target.IsValid)
            {
                return false;
            }
            return CanHitTargetFrom(caster.Position, target);
        }

        /// <summary>检查最大射程、直线视线和中途地图几何阻挡。</summary>
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo target)
        {
            if (caster == null || caster.Map == null || !target.IsValid)
            {
                return false;
            }
            if (!GenSight.LineOfSight(root, target.Cell, caster.Map))
            {
                return false;
            }
            return GroundDashUtility.CanReachRequestedCell(
                caster.Map,
                root,
                target.Cell,
                EffectiveRange);
        }

        /// <summary>绘制可用/不可用的鼠标指示。</summary>
        public override void OnGUI(LocalTargetInfo target)
        {
            if (CanHitTarget(target))
            {
                base.OnGUI(target);
            }
            else
            {
                GenUI.DrawMouseAttachment(TexCommand.CannotShoot);
            }
        }

        /// <summary>
        /// 绘制范围圈和有效目标格。
        /// 射程圈候选格经过完整路径解析，因此墙后不会继续显示。
        /// </summary>
        public override void DrawHighlight(LocalTargetInfo target)
        {
            if (caster == null || caster.Map == null)
            {
                return;
            }

            GroundDashPathResult targetPath;
            if (GroundDashUtility.TryResolvePath(
                caster.Map,
                caster.Position,
                target.Cell,
                EffectiveRange,
                out targetPath)
                && targetPath.ReachedRequestedCell)
            {
                GenDraw.DrawTargetHighlightWithLayer(
                    targetPath.EndCell.ToVector3Shifted(),
                    AltitudeLayer.MetaOverlays);
            }

            GenDraw.DrawRadiusRing(
                caster.Position,
                EffectiveRange,
                Color.white,
                delegate(IntVec3 cell)
                {
                    GroundDashPathResult path;
                    return GroundDashUtility.TryResolvePath(
                        caster.Map,
                        caster.Position,
                        cell,
                        EffectiveRange,
                        out path)
                        && path.ReachedRequestedCell;
                });
        }
    }
}
