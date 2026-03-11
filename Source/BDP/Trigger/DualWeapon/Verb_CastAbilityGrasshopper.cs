using RimWorld;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 蚱蜢能力的Verb - 指定使用自定义的PawnFlyer_Grasshopper
    /// </summary>
    public class Verb_CastAbilityGrasshopper : Verb_CastAbility
    {
        private float cachedEffectiveRange = -1f;

        public override bool MultiSelect => true;

        /// <summary>
        /// 关键：指定使用我们自定义的PawnFlyer
        /// </summary>
        public virtual ThingDef JumpFlyerDef => Core.BDP_DefOf.BDP_PawnFlyer_Grasshopper;

        public override float EffectiveRange
        {
            get
            {
                if (cachedEffectiveRange < 0f)
                {
                    if (base.EquipmentSource != null)
                    {
                        cachedEffectiveRange = base.EquipmentSource.GetStatValue(StatDefOf.JumpRange);
                    }
                    else
                    {
                        cachedEffectiveRange = verbProps.range;
                    }
                }
                return cachedEffectiveRange;
            }
        }

        protected override bool TryCastShot()
        {
            if (base.TryCastShot())
            {
                // 使用工具类，传入自定义PawnFlyer
                return GrasshopperUtility.DoJump(
                    CasterPawn,
                    currentTarget,
                    base.ReloadableCompSource,
                    verbProps,
                    ability,
                    base.CurrentTarget,
                    JumpFlyerDef  // 关键：传入自定义PawnFlyer
                );
            }
            return false;
        }

        public override void OnGUI(LocalTargetInfo target)
        {
            if (CanHitTarget(target) && GrasshopperUtility.ValidJumpTarget(caster.Map, target.Cell))
            {
                base.OnGUI(target);
            }
            else
            {
                GenUI.DrawMouseAttachment(TexCommand.CannotShoot);
            }
        }

        public override void OrderForceTarget(LocalTargetInfo target)
        {
            GrasshopperUtility.OrderJump(CasterPawn, target, this, EffectiveRange);
        }

        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            if (caster == null)
            {
                return false;
            }
            if (!CanHitTarget(target) || !GrasshopperUtility.ValidJumpTarget(caster.Map, target.Cell))
            {
                return false;
            }
            // ReloadableUtility在原版中不存在，跳过这个检查
            return true;
        }

        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            return GrasshopperUtility.CanHitTargetFrom(CasterPawn, root, targ, EffectiveRange);
        }

        public override void DrawHighlight(LocalTargetInfo target)
        {
            if (target.IsValid && GrasshopperUtility.ValidJumpTarget(caster.Map, target.Cell))
            {
                GenDraw.DrawTargetHighlightWithLayer(target.CenterVector3, AltitudeLayer.MetaOverlays);
            }
            GenDraw.DrawRadiusRing(caster.Position, EffectiveRange, UnityEngine.Color.white,
                (IntVec3 c) => GenSight.LineOfSight(caster.Position, c, caster.Map) && GrasshopperUtility.ValidJumpTarget(caster.Map, c));
        }
    }
}
