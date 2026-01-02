using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompAbilityRepulsiveArea : CompAbilityEffect
{
	public List<IntVec3> tmpCells = new List<IntVec3>();

	public new CompProperties_AbilityRepulsiveArea Props => (CompProperties_AbilityRepulsiveArea)props;

	public virtual float RadiusBase => Props.radius;

	public virtual int Distance => Props.distance;

	public Pawn Caster => parent.pawn;

	public override bool AICanTargetNow(LocalTargetInfo target)
	{
		return true;
	}

	public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
	{
		if (Props.targetOnCaster)
		{
			target = Caster;
		}
		List<Thing> list = new List<Thing>();
		foreach (IntVec3 item in AffectedCells(target.Cell))
		{
			list.AddRange(item.GetThingList(Caster.Map));
		}
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i] is Pawn pawn && IsPawnAffected(pawn))
			{
				ForceMovementUtility.ApplyRepulsiveForce(parent.pawn.PositionHeld, pawn, Distance, Props.removeHediffsAffected, ignoreResistance: true);
			}
		}
		if (Props.effecter != null)
		{
			Effecter effecter = new Effecter(Props.effecter);
			effecter.Trigger(new TargetInfo(target.Thing.Position, target.Thing.Map), TargetInfo.Invalid);
			effecter.Cleanup();
		}
	}

	public virtual bool IsPawnAffected(Pawn pawn)
	{
		if (Props.applyOnAllyOnly && pawn.Faction != Caster.Faction)
		{
			return false;
		}
		if (!Props.applyOnMech && pawn.RaceProps.IsMechanoid)
		{
			return false;
		}
		return true;
	}

	public List<IntVec3> AffectedCells(IntVec3 target)
	{
		tmpCells.Clear();
		foreach (IntVec3 item in GenRadial.RadialCellsAround(target, Props.radius, useCenter: true))
		{
			if (item.IsValid || item.InBounds(Caster.Map))
			{
				tmpCells.Add(item);
			}
		}
		tmpCells = tmpCells.Distinct().ToList();
		tmpCells.RemoveAll((IntVec3 cell) => !CanUseCell(cell));
		return tmpCells;
		bool CanUseCell(IntVec3 c)
		{
			if (!c.InBounds(Caster.Map))
			{
				return false;
			}
			return true;
		}
	}
}
