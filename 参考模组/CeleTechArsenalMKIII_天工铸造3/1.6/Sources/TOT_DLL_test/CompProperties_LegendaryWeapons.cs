using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TOT_DLL_test;

public class CompProperties_LegendaryWeapons : CompProperties
{
	public bool biocodeOnEquip = true;

	public List<AbilityDef> AbilitieDefs;

	public bool GivePE = true;

	public CompProperties_LegendaryWeapons()
	{
		compClass = typeof(CompLegendaryWeapons);
	}
}
