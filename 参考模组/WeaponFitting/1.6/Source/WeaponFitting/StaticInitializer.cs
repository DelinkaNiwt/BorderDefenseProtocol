using System.Linq;
using RimWorld;
using Verse;

namespace WeaponFitting;

[StaticConstructorOnStartup]
internal static class StaticInitializer
{
	static StaticInitializer()
	{
		foreach (ThingDef weaponfitting in ThingGenerator_WeaponFittings.ImpliedFittingDefs())
		{
			if (DefDatabase<ThingDef>.GetNamedSilentFail(weaponfitting.defName) == null)
			{
				DefGenerator.AddImpliedDef(weaponfitting);
				weaponfitting.ResolveReferences();
			}
		}
		Log.Message("FittingDefsLoaded");
		foreach (RecipeNeedsResolveDef def in DefDatabase<RecipeNeedsResolveDef>.AllDefs.ToList())
		{
			if (def.recipeDefs.NullOrEmpty())
			{
				continue;
			}
			foreach (RecipeDef recipe in def.recipeDefs)
			{
				recipe.ResolveReferences();
			}
		}
		foreach (ThingDef def2 in DefDatabase<ThingDef>.AllDefs.ToList())
		{
			if (!def2.inspectorTabs.NullOrEmpty() && Enumerable.Contains(def2.inspectorTabs, typeof(ITab_Storage)))
			{
				def2.ResolveReferences();
			}
		}
		ResourceCounter.ResetDefs();
	}
}
