using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AlienRace;

public class PawnBioDef : Def
{
	public BackstoryDef childhood;

	public BackstoryDef adulthood;

	public GenderPossibility gender;

	public NameTriple name;

	public List<ThingDef> validRaces;

	public bool factionLeader;

	public List<HediffDef> forcedHediffs = new List<HediffDef>();

	public List<ThingDefCountRangeClass> forcedItems = new List<ThingDefCountRangeClass>();

	public override IEnumerable<string> ConfigErrors()
	{
		if (childhood == null)
		{
			yield return "Error in " + defName + ": Childhood backstory not found";
		}
		if (adulthood == null)
		{
			yield return "Error in " + defName + ": Childhood backstory not found";
		}
		foreach (string item in base.ConfigErrors())
		{
			yield return item;
		}
	}

	public override void ResolveReferences()
	{
		base.ResolveReferences();
		PawnBio bio = new PawnBio
		{
			gender = gender,
			name = name,
			childhood = childhood,
			adulthood = adulthood,
			pirateKing = factionLeader
		};
		if (adulthood.spawnCategories.Count == 1 && adulthood.spawnCategories[0] == "Trader")
		{
			adulthood.spawnCategories.Add("Civil");
		}
		SolidBioDatabase.allBios.Add(bio);
	}
}
