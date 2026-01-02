using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompAbilityDamageBuildingArea : CompAbilityEffect
{
	public List<IntVec3> tmpCells = new List<IntVec3>();

	public new CompProperties_AbilityDamageBuildingArea Props => (CompProperties_AbilityDamageBuildingArea)props;

	public virtual float RadiusBase => Props.radius;

	public virtual int DamageAmountBase => Props.damageAmountBase;

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
			if (Props.fleckOnCell != null)
			{
				FleckMaker.Static(item, Caster.Map, Props.fleckOnCell);
			}
		}
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i] is Building building && IsBuildingAffected(building))
			{
				AncotUtility.DoDamage(building, Props.damageDef, Props.damageAmountBase, 1f, Caster);
			}
		}
		if (Props.effecter != null)
		{
			Effecter effecter = new Effecter(Props.effecter);
			effecter.Trigger(new TargetInfo(target.Thing.Position, target.Thing.Map), TargetInfo.Invalid);
			effecter.Cleanup();
		}
	}

	public virtual bool IsBuildingAffected(Building building)
	{
		if (!Props.applyOnAlly && building.Faction != Caster.Faction)
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
