using System.Collections.Generic;
using System.Linq;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded.Technomancer;

public static class PsyringUtilities
{
	public static IEnumerable<Psyring> AllPsyrings(this Pawn pawn)
	{
		return pawn.apparel.WornApparel.OfType<Psyring>();
	}

	public static IEnumerable<AbilityDef> AllAbilitiesFromPsyrings(this Pawn pawn)
	{
		return (from psyring in pawn.AllPsyrings()
			where psyring.Added
			select psyring.Ability).Distinct();
	}

	public static IEnumerable<PsycasterPathDef> AllPathsFromPsyrings(this Pawn pawn)
	{
		return (from psyring in pawn.AllPsyrings()
			select psyring.Path).Distinct();
	}
}
