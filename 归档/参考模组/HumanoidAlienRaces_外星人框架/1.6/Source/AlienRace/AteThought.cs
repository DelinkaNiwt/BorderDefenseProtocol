using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AlienRace;

public class AteThought
{
	public List<ThingDef> raceList;

	[LoadDefFromField("AteHumanlikeMeatDirect")]
	public ThoughtDef thought;

	[LoadDefFromField("AteHumanlikeMeatDirectCannibal")]
	public ThoughtDef thoughtCannibal;

	[LoadDefFromField("AteHumanlikeMeatAsIngredient")]
	public ThoughtDef ingredientThought;

	[LoadDefFromField("AteHumanlikeMeatAsIngredientCannibal")]
	public ThoughtDef ingredientThoughtCannibal;

	public ThoughtDef GetThought(bool cannibal, bool ingredient)
	{
		if (!cannibal)
		{
			if (!ingredient)
			{
				return thought;
			}
			return ingredientThought;
		}
		if (!ingredient)
		{
			return thoughtCannibal;
		}
		return ingredientThoughtCannibal;
	}
}
