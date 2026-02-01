using System.Collections.Generic;
using RimWorld;
using Verse;

namespace NCL;

public class HediffCompProperties_TotalClear : HediffCompProperties
{
	public List<FactionDef> whiteFactions = new List<FactionDef>();

	public List<ThingDef> whiteRaces = new List<ThingDef>();

	public List<PawnKindDef> whitePawnKinds = new List<PawnKindDef>();

	public List<ThingDef> whiteThings = new List<ThingDef>();

	public int clearInterval = 60;

	public HediffCompProperties_TotalClear()
	{
		compClass = typeof(HediffComp_TotalClear);
	}
}
