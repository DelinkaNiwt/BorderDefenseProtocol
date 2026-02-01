using System.Collections.Generic;
using RimWorld;
using Verse;

namespace NCL;

public class HediffComp_TotalClear : HediffComp
{
	private int ticksUntilClear;

	public HediffCompProperties_TotalClear Props => (HediffCompProperties_TotalClear)props;

	public override void CompPostTick(ref float severityAdjustment)
	{
		base.CompPostTick(ref severityAdjustment);
		if (base.Pawn.Map != null && base.Pawn.Spawned && --ticksUntilClear <= 0)
		{
			PerformTotalClear();
			ticksUntilClear = Props.clearInterval;
		}
	}

	private void PerformTotalClear()
	{
		CellRect rect = CellRect.CenteredOn(base.Pawn.Position, 3);
		List<Thing> things = new List<Thing>();
		foreach (IntVec3 cell in rect)
		{
			if (cell.InBounds(base.Pawn.Map))
			{
				things.AddRange(cell.GetThingList(base.Pawn.Map));
			}
		}
		foreach (Thing thing in things)
		{
			if (ShouldClear(thing))
			{
				ExecuteClear(thing);
			}
		}
	}

	private bool ShouldClear(Thing thing)
	{
		if (thing is Pawn pawn)
		{
			if (Props.whiteFactions.Contains(pawn.Faction?.def))
			{
				return false;
			}
			if (Props.whiteRaces.Contains(pawn.def))
			{
				return false;
			}
			if (Props.whitePawnKinds.Contains(pawn.kindDef))
			{
				return false;
			}
		}
		if (Props.whiteThings.Contains(thing.def))
		{
			return false;
		}
		return true;
	}

	private void ExecuteClear(Thing thing)
	{
		if (thing.Destroyed)
		{
			return;
		}
		if (thing is Pawn targetPawn)
		{
			if (!targetPawn.Dead)
			{
				DamageInfo dinfo = new DamageInfo(DamageDefOf.Vaporize, 99999f, 0f, -1f, base.Pawn);
				targetPawn.Kill(dinfo);
			}
		}
		else if (thing.def.destroyable)
		{
			thing.Destroy();
		}
		else
		{
			thing.DeSpawn();
		}
		FleckMaker.ThrowLightningGlow(thing.DrawPos, thing.Map, 2f);
	}
}
