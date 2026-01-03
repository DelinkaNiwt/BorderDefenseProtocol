using System.Collections.Generic;
using RimWorld;
using Verse;

namespace WeaponFitting;

public class UniqueWeaponCategoriesDef : Def
{
	public List<ThingDef> weaponDefs = new List<ThingDef>();

	public List<WeaponCategoryDef> weaponCategories = new List<WeaponCategoryDef>();

	public int? maxTraits = null;
}
