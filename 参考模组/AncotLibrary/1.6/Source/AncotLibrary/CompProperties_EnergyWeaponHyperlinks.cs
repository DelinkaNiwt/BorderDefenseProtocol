using System.Collections.Generic;
using System.Linq;
using Verse;

namespace AncotLibrary;

public class CompProperties_EnergyWeaponHyperlinks : CompProperties
{
	public CompProperties_EnergyWeaponHyperlinks()
	{
		compClass = typeof(CompEnergyWeaponHyperlinks);
	}

	public override void ResolveReferences(ThingDef parentDef)
	{
		if (parentDef.descriptionHyperlinks == null)
		{
			parentDef.descriptionHyperlinks = new List<DefHyperlink>();
		}
		parentDef.descriptionHyperlinks.Clear();
		List<ThingDef> list = DefDatabase<ThingDef>.AllDefsListForReading.ToList();
		for (int i = 0; i < list.Count; i++)
		{
			ThingDef thingDef = list[i];
			if (thingDef != null && thingDef.HasComp<CompWeaponCharge>())
			{
				parentDef.descriptionHyperlinks.Add(thingDef);
			}
		}
	}
}
