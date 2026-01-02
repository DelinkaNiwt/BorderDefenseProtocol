using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Milira;

public class CompTargetableWeapon : CompTargetable
{
	protected override bool PlayerChoosesTarget => true;

	public CompProperties_TargetableWeapon Props_Milian => (CompProperties_TargetableWeapon)props;

	public bool ShouldBeRangeWeapon => Props_Milian.shouldBeRangeWeapon;

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		base.PostSpawnSetup(respawningAfterLoad);
	}

	public bool Available(Thing thing, Pawn pawn)
	{
		if (thing == null)
		{
			return false;
		}
		if (ModsConfig.IsActive("Ancot.MilianModification") && pawn.health.hediffSet.GetFirstHediffOfDef(MiliraDefOf.MilianFitting_AdaptiveArmSystem) != null && thing.def.IsWeapon)
		{
			return true;
		}
		if (ShouldBeRangeWeapon && thing.def.IsRangedWeapon)
		{
			return true;
		}
		if (!ShouldBeRangeWeapon && thing.def.IsMeleeWeapon)
		{
			return true;
		}
		return false;
	}

	protected override TargetingParameters GetTargetingParameters()
	{
		return new TargetingParameters
		{
			canTargetItems = true,
			mapObjectTargetsMustBeAutoAttackable = false,
			canTargetSelf = false,
			canTargetBuildings = false,
			canTargetPawns = false,
			thingCategory = ThingCategory.Item,
			validator = delegate(TargetInfo target)
			{
				Pawn pawn = parent as Pawn;
				return Available(target.Thing, pawn);
			}
		};
	}

	public override IEnumerable<Thing> GetTargets(Thing targetChosenByPlayer = null)
	{
		yield return targetChosenByPlayer;
	}
}
