using System.Collections.Generic;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded;

public static class AbilityExtensionPsycastUtility
{
	private static readonly Dictionary<AbilityDef, AbilityExtension_Psycast> cache = new Dictionary<AbilityDef, AbilityExtension_Psycast>();

	public static AbilityExtension_Psycast Psycast(this AbilityDef def)
	{
		if (cache.TryGetValue(def, out var value))
		{
			return value;
		}
		value = ((Def)(object)def).GetModExtension<AbilityExtension_Psycast>();
		cache[def] = value;
		return value;
	}
}
