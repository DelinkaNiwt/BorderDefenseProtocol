using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompRepairMechArea : ThingComp
{
	private CompProperties_RepairMechArea Props => (CompProperties_RepairMechArea)props;

	public override void CompTickInterval(int delta)
	{
		base.CompTickInterval(delta);
		if (!parent.IsHashIntervalTick(Props.repairTicks, delta))
		{
			return;
		}
		List<Thing> list = new List<Thing>();
		foreach (IntVec3 item in AncotUtility.AffectedCellsRadial(parent.Position, parent.Map, Props.radius, useCenter: true))
		{
			list.AddRange(item.GetThingList(parent.Map));
		}
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i] is Pawn pawn && IsPawnAffected(pawn))
			{
				for (int j = 0; j < Props.hitpointPerRepair; j++)
				{
					MechRepairUtility.RepairTick(pawn, 1);
				}
			}
		}
	}

	public virtual bool IsPawnAffected(Pawn pawn)
	{
		if (Props.applyAllyOnly && pawn.Faction != parent.Faction)
		{
			return false;
		}
		if (!pawn.RaceProps.IsMechanoid)
		{
			return false;
		}
		return true;
	}
}
