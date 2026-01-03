using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Milira;

public class StorytellerComp_RandomMiliraQuest : StorytellerComp_OnOffCycle
{
	public List<QuestScriptDef> questScriptDefs = new List<QuestScriptDef>();

	public StorytellerComp_RandomMiliraQuest()
	{
		questScriptDefs.Add(MiliraDefOf.Milira_Cluster_WorldMap);
		questScriptDefs.Add(MiliraDefOf.Milira_Milian_WorldMap);
		questScriptDefs.Add(MiliraDefOf.Milira_MilianSpecific_WorldMap);
		if (ModsConfig.RoyaltyActive)
		{
		}
		if (ModsConfig.IdeologyActive)
		{
		}
		if (!ModsConfig.BiotechActive)
		{
		}
	}

	public override IncidentParms GenerateParms(IncidentCategoryDef incCat, IIncidentTarget target)
	{
		IncidentParms incidentParms = base.GenerateParms(incCat, target);
		QuestScriptDef questScriptDef = questScriptDefs.RandomElement();
		incidentParms.questScriptDef = questScriptDef;
		return incidentParms;
	}
}
