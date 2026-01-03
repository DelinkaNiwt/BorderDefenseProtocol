using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AncotLibrary;

public class WeaponFittingDef : Def
{
	public WeaponCategoryDef weaponCategoryDef;

	public List<WeaponCategoryDef> weaponCategoryDefs = new List<WeaponCategoryDef>();

	public ThingCategoryDef thingCategoryDef;

	public string texPath;
}
