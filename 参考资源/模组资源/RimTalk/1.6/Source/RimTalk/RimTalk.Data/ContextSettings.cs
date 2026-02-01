using Verse;

namespace RimTalk.Data;

public class ContextSettings : IExposable
{
	public bool EnableContextOptimization = false;

	public int MaxPawnContextCount = 3;

	public int ConversationHistoryCount = 1;

	public bool IncludeRace = true;

	public bool IncludeNotableGenes = true;

	public bool IncludeIdeology = true;

	public bool IncludeBackstory = true;

	public bool IncludeTraits = true;

	public bool IncludeSkills = true;

	public bool IncludeHealth = true;

	public bool IncludeMood = true;

	public bool IncludeThoughts = true;

	public bool IncludeRelations = true;

	public bool IncludeEquipment = true;

	public bool IncludePrisonerSlaveStatus = false;

	public bool IncludeTime = true;

	public bool IncludeDate = false;

	public bool IncludeSeason = true;

	public bool IncludeWeather = true;

	public bool IncludeLocationAndTemperature = true;

	public bool IncludeTerrain = false;

	public bool IncludeBeauty = false;

	public bool IncludeCleanliness = false;

	public bool IncludeSurroundings = false;

	public bool IncludeWealth = false;

	public void ExposeData()
	{
		Scribe_Values.Look(ref EnableContextOptimization, "EnableContextOptimization", defaultValue: false);
		Scribe_Values.Look(ref MaxPawnContextCount, "MaxPawnContextCount", 3);
		Scribe_Values.Look(ref ConversationHistoryCount, "ConversationHistoryCount", 1);
		Scribe_Values.Look(ref IncludeRace, "IncludeRace", defaultValue: true);
		Scribe_Values.Look(ref IncludeNotableGenes, "IncludeNotableGenes", defaultValue: true);
		Scribe_Values.Look(ref IncludeIdeology, "IncludeIdeology", defaultValue: true);
		Scribe_Values.Look(ref IncludeBackstory, "IncludeBackstory", defaultValue: true);
		Scribe_Values.Look(ref IncludeTraits, "IncludeTraits", defaultValue: true);
		Scribe_Values.Look(ref IncludeSkills, "IncludeSkills", defaultValue: true);
		Scribe_Values.Look(ref IncludeHealth, "IncludeHealth", defaultValue: true);
		Scribe_Values.Look(ref IncludeMood, "IncludeMood", defaultValue: true);
		Scribe_Values.Look(ref IncludeThoughts, "IncludeThoughts", defaultValue: true);
		Scribe_Values.Look(ref IncludeRelations, "IncludeRelations", defaultValue: true);
		Scribe_Values.Look(ref IncludeEquipment, "IncludeEquipment", defaultValue: true);
		Scribe_Values.Look(ref IncludePrisonerSlaveStatus, "IncludePrisonerSlaveStatus", defaultValue: false);
		Scribe_Values.Look(ref IncludeTime, "IncludeTime", defaultValue: true);
		Scribe_Values.Look(ref IncludeDate, "IncludeDate", defaultValue: false);
		Scribe_Values.Look(ref IncludeSeason, "IncludeSeason", defaultValue: true);
		Scribe_Values.Look(ref IncludeWeather, "IncludeWeather", defaultValue: true);
		Scribe_Values.Look(ref IncludeLocationAndTemperature, "IncludeLocationAndTemperature", defaultValue: true);
		Scribe_Values.Look(ref IncludeTerrain, "IncludeTerrain", defaultValue: false);
		Scribe_Values.Look(ref IncludeBeauty, "IncludeBeauty", defaultValue: false);
		Scribe_Values.Look(ref IncludeCleanliness, "IncludeCleanliness", defaultValue: false);
		Scribe_Values.Look(ref IncludeSurroundings, "IncludeSurroundings", defaultValue: false);
		Scribe_Values.Look(ref IncludeWealth, "IncludeWealth", defaultValue: false);
	}
}
