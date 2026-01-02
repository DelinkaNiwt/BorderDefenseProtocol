using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompAbilityShowRange : CompAbilityEffect
{
	public new CompProperties_AbilityShowRange Props => (CompProperties_AbilityShowRange)props;

	public override bool AICanTargetNow(LocalTargetInfo target)
	{
		return true;
	}

	public override void DrawEffectPreview(LocalTargetInfo target)
	{
		List<IntVec3> list = new List<IntVec3>();
		foreach (IntVec3 item in GenRadial.RadialCellsAround(target.Cell, Props.range, useCenter: true))
		{
			if (item.IsValid || item.InBounds(parent.pawn.Map))
			{
				list.Add(item);
			}
		}
		if (Props.minRange != 0f)
		{
			foreach (IntVec3 item2 in GenRadial.RadialCellsAround(target.Cell, Props.minRange, useCenter: true))
			{
				if (item2.IsValid || item2.InBounds(parent.pawn.Map))
				{
					list.Add(item2);
				}
			}
		}
		GenDraw.DrawFieldEdges(list);
	}
}
