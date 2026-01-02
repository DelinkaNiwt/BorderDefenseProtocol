using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Milira;

public class CompProperties_TargetableMilian : CompProperties_Targetable
{
	public List<PawnKindDef> targetableMilianPawnkinds = new List<PawnKindDef>();

	public List<HediffDef> availableIfWithHediff = new List<HediffDef>();

	public CompProperties_TargetableMilian()
	{
		compClass = typeof(CompTargetable_Milian);
	}
}
