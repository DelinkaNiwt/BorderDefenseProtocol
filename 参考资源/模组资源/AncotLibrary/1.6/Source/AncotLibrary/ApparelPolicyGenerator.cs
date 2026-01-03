using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace AncotLibrary;

public class ApparelPolicyGenerator
{
	public static List<FloatMenuOption> ApparelPolicyMenu()
	{
		List<FloatMenuOption> list = new List<FloatMenuOption>();
		foreach (ApparelPolicyDef apparelPolicyDef in DefDatabase<ApparelPolicyDef>.AllDefs)
		{
			list.Add(new FloatMenuOption(apparelPolicyDef.label, delegate
			{
				if (Current.Game == null)
				{
					Messages.Message("Ancot.ApparelPolicyGenerateFailed_MustInGame".Translate(), MessageTypeDefOf.NegativeEvent);
				}
				else
				{
					GenerateApparelPolicyFromDef(apparelPolicyDef, out var apparelPolicy);
					GameComponent_AncotLibrary.GC.raceApparelPolicy[apparelPolicyDef.race] = apparelPolicy;
					Messages.Message("Ancot.ApparelPolicyGenerateSuccess".Translate(apparelPolicyDef.label), MessageTypeDefOf.PositiveEvent);
				}
			}));
		}
		return list;
	}

	public static bool GenerateApparelPolicyFromDef(out Dictionary<ThingDef, ApparelPolicy> raceApparelPolicy)
	{
		raceApparelPolicy = new Dictionary<ThingDef, ApparelPolicy>();
		if (!AncotLibrarySettings.apparelPolicy_GenerateAtStart)
		{
			return false;
		}
		foreach (ApparelPolicyDef allDef in DefDatabase<ApparelPolicyDef>.AllDefs)
		{
			GenerateApparelPolicyFromDef(allDef, out var apparelPolicy);
			raceApparelPolicy.Add(allDef.race, apparelPolicy);
		}
		return true;
	}

	public static bool GenerateApparelPolicyFromDef(ThingDef race, out ApparelPolicy apparelPolicy)
	{
		apparelPolicy = new ApparelPolicy();
		foreach (ApparelPolicyDef allDef in DefDatabase<ApparelPolicyDef>.AllDefs)
		{
			if (allDef.race == race)
			{
				GenerateApparelPolicyFromDef(allDef, out apparelPolicy);
				GameComponent_AncotLibrary.GC.raceApparelPolicy[race] = apparelPolicy;
				return true;
			}
		}
		return false;
	}

	public static void GenerateApparelPolicyFromDef(ApparelPolicyDef apparelPolicyDef, out ApparelPolicy apparelPolicy)
	{
		apparelPolicy = Current.Game.outfitDatabase.MakeNewOutfit();
		apparelPolicy.label = apparelPolicyDef.label;
		apparelPolicy.filter.SetDisallowAll();
		apparelPolicy.filter.SetAllow(SpecialThingFilterDefOf.AllowDeadmansApparel, allow: false);
		IEnumerable<ThingDef> apparels = apparelPolicyDef.apparels;
		IEnumerable<ThingDef> source = apparels ?? Enumerable.Empty<ThingDef>();
		IEnumerable<ThingCategoryDef> apparelCategories = apparelPolicyDef.apparelCategories;
		IEnumerable<ThingCategoryDef> source2 = apparelCategories ?? Enumerable.Empty<ThingCategoryDef>();
		IEnumerable<string> apparelTags = apparelPolicyDef.apparelTags;
		IEnumerable<string> source3 = apparelTags ?? Enumerable.Empty<string>();
		apparels = apparelPolicyDef.excludeApparels;
		IEnumerable<ThingDef> source4 = apparels ?? Enumerable.Empty<ThingDef>();
		apparelCategories = apparelPolicyDef.excludeApparelCategories;
		IEnumerable<ThingCategoryDef> source5 = apparelCategories ?? Enumerable.Empty<ThingCategoryDef>();
		apparelTags = apparelPolicyDef.excludeApparelTags;
		IEnumerable<string> source6 = apparelTags ?? Enumerable.Empty<string>();
		HashSet<ThingCategoryDef> hashSet = source2.SelectMany(GetChildCategories).ToHashSet();
		HashSet<ThingCategoryDef> hashSet2 = source5.SelectMany(GetChildCategories).ToHashSet();
		foreach (ThingDef allDef in DefDatabase<ThingDef>.AllDefs)
		{
			if (allDef.apparel == null)
			{
				continue;
			}
			if (apparelPolicyDef.includeApparelUtility && allDef.thingCategories.NotNullAndContains(ThingCategoryDefOf.ApparelUtility))
			{
				apparelPolicy.filter.SetAllow(allDef, allow: true);
				continue;
			}
			apparelCategories = allDef.thingCategories;
			IEnumerable<ThingCategoryDef> other = apparelCategories ?? Enumerable.Empty<ThingCategoryDef>();
			apparelTags = allDef.apparel.tags;
			IEnumerable<string> thingTags = apparelTags ?? Enumerable.Empty<string>();
			bool flag = source.Contains(allDef) || hashSet.Overlaps(other) || source3.Any((string tag) => thingTags.Contains(tag));
			if (source4.Contains(allDef) || hashSet2.Overlaps(other) || source6.Any((string tag) => thingTags.Contains(tag)))
			{
				apparelPolicy.filter.SetAllow(allDef, allow: false);
			}
			else if (flag)
			{
				apparelPolicy.filter.SetAllow(allDef, allow: true);
			}
		}
	}

	public static IEnumerable<ThingCategoryDef> GetChildCategories(ThingCategoryDef parent)
	{
		yield return parent;
		if (parent.childCategories == null)
		{
			yield break;
		}
		foreach (ThingCategoryDef child in parent.childCategories)
		{
			foreach (ThingCategoryDef childCategory in GetChildCategories(child))
			{
				yield return childCategory;
			}
		}
	}
}
