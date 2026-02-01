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
	public class CompProperties_PlaceShieldAbility : CompProperties_AbilityEffect
	{
		public CompProperties_PlaceShieldAbility()
		{
			this.compClass = typeof(CompAbilityEffect_PlaceShield);
		}
	}

	public class CompAbilityEffect_PlaceShield : CompAbilityEffect
	{
		public new CompProperties_PlaceShieldAbility Props
		{
			get
			{
				return this.props as CompProperties_PlaceShieldAbility;
			}
		}
		public float Power
        {
            get
            {
				Pawn p = this.parent.pawn;
				if (p != null && p.needs.energy != null)
                {
					return p.needs.energy.CurLevel;
                }
				return 0;
            }
        }
		public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
		{
			this.PlaceShield(this.parent.pawn);
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
		private void PlaceShield(Pawn pawn)
		{
			if (pawn.needs.energy == null)
            {
				return;
            }
			if (pawn.needs.energy.CurLevel <= 20f)
            {
				pawn.needs.energy.CurLevel = 0;
            }
            else
            {
				pawn.needs.energy.CurLevel -= 20f;
            }
			IntVec3 pos = pawn.Position;
			Map map = pawn.Map;
			if (pos.IsValid && pos.InBounds(map))
			{
				ThingDef thingDef = GDDefOf.GD_AbilityShieldProjector;
				Thing shield = ThingMaker.MakeThing(thingDef, null);
				if (pawn.Faction != null)
                {
					shield.SetFaction(pawn.Faction);
				}
				GenPlace.TryPlaceThing(shield, pos, map, ThingPlaceMode.Near, null, null, default(Rot4));
				CompAbilityEffect_PlaceShield.SpawnEffect(shield);
			}
		}
		private static void SpawnEffect(Thing projector)
		{
			FleckMaker.Static(projector.TrueCenter(), projector.Map, FleckDefOf.BroadshieldActivation, 1f);
			SoundDefOf.Broadshield_Startup.PlayOneShot(new TargetInfo(projector.Position, projector.Map, false));
		}
		public override bool GizmoDisabled(out string reason)
		{
			reason = null;
			Pawn pawn = this.parent.pawn;
			if (MechanitorUtility.GetOverseer(pawn) == null)
            {
				reason = "GD.MechanitorNotFound".Translate();
				return true;
            }
			if (pawn.Downed)
			{
				reason = "GD.MechanoidDowned".Translate();
				return true;
			}
			if (this.Power < 20f)
			{
				reason = "GD.LowEnergy".Translate();
				return true;
			}
			return false;
		}
	}
}