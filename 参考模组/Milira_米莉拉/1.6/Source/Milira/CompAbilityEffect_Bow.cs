using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Milira;

public class CompAbilityEffect_Bow : CompAbilityEffect
{
	private List<IntVec3> tmpCells = new List<IntVec3>();

	private Pawn Pawn => parent.pawn;

	public new CompProperties_AbilityBow Props => (CompProperties_AbilityBow)props;

	public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
	{
		base.Apply(AimingTarget(Pawn, target), dest);
		for (int i = 0; (float)i < Props.amount; i++)
		{
			SkyfallerMaker.SpawnSkyfaller(Props.skyfallerDef, AffectedCells(target.Cell, Props.radius).RandomElementWithFallback(), Pawn.Map);
		}
		if (Props.sprayEffecter != null)
		{
			Props.sprayEffecter.Spawn(parent.pawn.Position, target.Cell, parent.pawn.Map).Cleanup();
		}
	}

	public override IEnumerable<PreCastAction> GetPreCastActions()
	{
		Log.Message("GetPreCastActions");
		return Enumerable.Empty<PreCastAction>();
	}

	public override IEnumerable<Mote> CustomWarmupMotes(LocalTargetInfo target)
	{
		Log.Message("Mote");
		return Enumerable.Empty<Mote>();
	}

	public override void DrawEffectPreview(LocalTargetInfo target)
	{
		GenDraw.DrawFieldEdges(AffectedCells(target.Cell, Props.radius));
	}

	public override bool AICanTargetNow(LocalTargetInfo target)
	{
		if (Pawn.Faction != null)
		{
			foreach (IntVec3 item in AffectedCells(target.Cell, Props.radius))
			{
				List<Thing> thingList = item.GetThingList(Pawn.Map);
				for (int i = 0; i < thingList.Count; i++)
				{
					if (thingList[i].Faction == Pawn.Faction)
					{
						return false;
					}
				}
			}
		}
		return true;
	}

	private List<IntVec3> AffectedCells(IntVec3 target, float radius)
	{
		tmpCells.Clear();
		foreach (IntVec3 item in GenRadial.RadialCellsAround(target, radius, useCenter: true))
		{
			if (item.IsValid || item.InBounds(Pawn.Map))
			{
				tmpCells.Add(item);
			}
		}
		tmpCells = tmpCells.Distinct().ToList();
		tmpCells.RemoveAll((IntVec3 cell) => !CanUseCell(cell));
		return tmpCells;
		bool CanUseCell(IntVec3 c)
		{
			if (!c.InBounds(Pawn.Map))
			{
				return false;
			}
			return true;
		}
	}

	public IntVec3 AimingTarget(Pawn pawn, LocalTargetInfo currentTarget)
	{
		IntVec3 position = pawn.Position;
		position.y++;
		return position;
	}
}
