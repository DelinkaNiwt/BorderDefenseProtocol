using System.Collections.Generic;
using RimWorld;

namespace AncotLibrary;

public class StorytellerCompProperties_OnOffCycle_ExtraQuest : StorytellerCompProperties_OnOffCycle
{
	public List<QuestScriptDef> questScriptDefs;

	public StorytellerCompProperties_OnOffCycle_ExtraQuest()
	{
		compClass = typeof(StorytellerComp_OnOffCycle_ExtraQuest);
	}
}
