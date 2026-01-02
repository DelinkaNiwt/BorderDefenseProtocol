using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AncotLibrary;

public class StorytellerComp_OnOffCycle_ExtraQuest : StorytellerComp_OnOffCycle
{
	protected StorytellerCompProperties_OnOffCycle_ExtraQuest Props_Extra => (StorytellerCompProperties_OnOffCycle_ExtraQuest)props;

	public List<QuestScriptDef> QuestScriptDefs => Props_Extra.questScriptDefs;

	public override IncidentParms GenerateParms(IncidentCategoryDef incCat, IIncidentTarget target)
	{
		IncidentParms incidentParms = base.GenerateParms(incCat, target);
		QuestScriptDef questScriptDef = QuestScriptDefs.RandomElement();
		incidentParms.questScriptDef = questScriptDef;
		return incidentParms;
	}
}
