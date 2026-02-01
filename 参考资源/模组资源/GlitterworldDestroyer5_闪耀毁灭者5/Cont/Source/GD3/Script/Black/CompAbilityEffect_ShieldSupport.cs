using System;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.Sound;
using System.Collections.Generic;

namespace GD3
{
	public class CompAbilityEffect_ShieldSupport : CompAbilityEffect
	{
		public new CompProperties_ShieldSupportAbility Props
		{
			get
			{
				return this.props as CompProperties_ShieldSupportAbility;
			}
		}
		public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
		{
			this.ShieldSupport(target.Pawn);
		}
		public override void Apply(GlobalTargetInfo target)
		{
			this.Apply(null, null);
		}
		public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
		{
			return true;
		}

		public override bool CanApplyOn(GlobalTargetInfo target)
		{
			return this.CanApplyOn(null, null);
		}

		private void ShieldSupport(Pawn pawn)
		{
			if (pawn == null)
            {
				return;
            }
			Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(GDDefOf.GD_BlackShield, false);
			if (hediff == null)
			{
				hediff = pawn.health.AddHediff(GDDefOf.GD_BlackShield, pawn.health.hediffSet.GetBrain(), null, null);
				hediff.Severity = 1f;
			}
			HediffComp_Disappears hediffComp_Disappears = hediff.TryGetComp<HediffComp_Disappears>();
			if (hediffComp_Disappears != null)
			{
				hediffComp_Disappears.ticksToDisappear = 721;
			}
			SpawnEffect(pawn);

		}

		private static void SpawnEffect(Thing projector)
		{
			FleckMaker.Static(projector.TrueCenter(), projector.Map, FleckDefOf.BroadshieldActivation, 0.6f);
			SoundDefOf.Broadshield_Startup.PlayOneShot(new TargetInfo(projector.Position, projector.Map, false));
		}

		public override bool GizmoDisabled(out string reason)
		{
			reason = null;
			return false;
		}
	}
}