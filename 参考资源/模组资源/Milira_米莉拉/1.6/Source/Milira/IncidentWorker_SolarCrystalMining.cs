using RimWorld;
using Verse;

namespace Milira;

public class IncidentWorker_SolarCrystalMining : IncidentWorker
{
	protected override bool CanFireNowSub(IncidentParms parms)
	{
		return ((Map)parms.target).gameConditionManager.ConditionIsActive(MiliraDefOf.SolarFlare);
	}

	protected override bool TryExecuteWorker(IncidentParms parms)
	{
		QuestScriptDef obj = def.questScriptDef ?? parms.questScriptDef ?? NaturalRandomQuestChooser.ChooseNaturalRandomQuest(parms.points, parms.target);
		QuestScriptDef questDef = obj;
		parms.questScriptDef = obj;
		GiveQuest(parms, questDef);
		return true;
	}

	protected virtual void GiveQuest(IncidentParms parms, QuestScriptDef questDef)
	{
		Quest quest = QuestUtility.GenerateQuestAndMakeAvailable(questDef, parms.points);
		if (!quest.hidden && questDef.sendAvailableLetter)
		{
			QuestUtility.SendLetterQuestAvailable(quest);
		}
	}
}
