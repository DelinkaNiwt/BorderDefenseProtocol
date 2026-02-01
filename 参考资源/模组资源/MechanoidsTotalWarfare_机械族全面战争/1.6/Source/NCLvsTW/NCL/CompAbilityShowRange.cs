using System.Collections.Generic;
using RimWorld;
using Verse;

namespace NCL;

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
		foreach (IntVec3 intVec in GenRadial.RadialCellsAround(target.Cell, Props.range, useCenter: true))
		{
			if (intVec.IsValid || intVec.InBounds(parent.pawn.Map))
			{
				list.Add(intVec);
			}
		}
		if (Props.minRange != 0f)
		{
			foreach (IntVec3 intVec2 in GenRadial.RadialCellsAround(target.Cell, Props.minRange, useCenter: true))
			{
				if (intVec2.IsValid || intVec2.InBounds(parent.pawn.Map))
				{
					list.Add(intVec2);
				}
			}
		}
		GenDraw.DrawFieldEdges(list);
	}
}
