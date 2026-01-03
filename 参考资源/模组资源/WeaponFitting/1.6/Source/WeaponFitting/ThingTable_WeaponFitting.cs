using System;
using System.Collections.Generic;
using System.Linq;
using AncotLibrary;
using Verse;

namespace WeaponFitting;

public class ThingTable_WeaponFitting : ThingTable
{
	public ThingTable_WeaponFitting(ThingTableDef def, Func<IEnumerable<Thing>> ThingsGetter, int uiWidth, int uiHeight)
		: base(def, ThingsGetter, uiWidth, uiHeight)
	{
	}

	protected override IEnumerable<Thing> LabelSortFunction(IEnumerable<Thing> input)
	{
		return from thing in input
			orderby !thing.IsWeaponStar(), thing.WeaponOwner() == null, thing.Label
			select thing;
	}
}
