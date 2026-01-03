using System.Collections.Generic;
using Verse;

namespace AncotLibrary;

public class ApparelPolicyDef : Def
{
	public ThingDef race;

	public List<ThingDef> apparels;

	public List<string> apparelTags;

	public List<ThingCategoryDef> apparelCategories;

	public List<ThingDef> excludeApparels;

	public List<string> excludeApparelTags;

	public List<ThingCategoryDef> excludeApparelCategories;

	public bool includeApparelUtility = false;
}
