using System;
using System.Collections.Generic;
using Verse;
using RimWorld;

namespace GD3
{
	public class CompProperties_AbilityFireBurstB : CompProperties_AbilityEffect
	{
		public CompProperties_AbilityFireBurstB()
		{
			this.compClass = typeof(CompAbilityEffect_FireBurstB);
		}

		public float radius = 6f;
	}

	public class CompAbilityEffect_FireBurstB : CompAbilityEffect
	{
		private new CompProperties_AbilityFireBurstB Props
		{
			get
			{
				return (CompProperties_AbilityFireBurstB)this.props;
			}
		}

		private Pawn Pawn
		{
			get
			{
				return this.parent.pawn;
			}
		}

		public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
		{
			GenExplosion.DoExplosion(this.Pawn.Position, this.Pawn.MapHeld, this.Props.radius, GDDefOf.VaporizeSec, this.Pawn, -1, -1f, null, null, null, null, ThingDefOf.Filth_Fuel, 1f, 1, null, null, 255, false, null, 0f, 1, 1f, false, null, null, null, false, 0.6f, 0f, true, null, 1f);
			base.Apply(target, dest);
		}

		public override IEnumerable<PreCastAction> GetPreCastActions()
		{
			yield return new PreCastAction
			{
				action = delegate (LocalTargetInfo a, LocalTargetInfo b)
				{
					this.parent.AddEffecterToMaintain(EffecterDefOf.Fire_Burst.Spawn(this.parent.pawn.Position, this.parent.pawn.Map, 1f), this.parent.pawn.Position, 17, this.parent.pawn.Map);
				},
				ticksAwayFromCast = 17
			};
			yield break;
		}

		public override bool AICanTargetNow(LocalTargetInfo target)
		{
			Pawn pawn;
			return this.Pawn.Faction != Faction.OfPlayer && (target.HasThing && (pawn = (target.Thing as Pawn)) != null) && pawn.TargetCurrentlyAimingAt == this.Pawn;
		}

		public override void CompTick()
		{
			if (this.parent.Casting)
			{
				FireBurstUtility.ThrowFuelTick(this.Pawn.Position, this.Props.radius, this.Pawn.Map);
			}
		}
	}
}