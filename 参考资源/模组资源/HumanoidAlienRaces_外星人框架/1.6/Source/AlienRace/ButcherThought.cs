using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AlienRace;

public class ButcherThought
{
	public List<ThingDef> raceList;

	[LoadDefFromField("ButcheredHumanlikeCorpse")]
	public ThoughtDef thought;

	[LoadDefFromField("KnowButcheredHumanlikeCorpse")]
	public ThoughtDef knowThought;
}
