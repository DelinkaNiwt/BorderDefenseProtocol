using RimWorld;
using Verse;

namespace Milira;

public class IncidentWorker_GiveQuestExceptMiliraScenario : IncidentWorker
{
	protected override bool CanFireNowSub(IncidentParms parms)
	{
		if (!base.CanFireNowSub(parms))
		{
			return false;
		}
		if (def.questScriptDef != null)
		{
			if (!def.questScriptDef.CanRun(parms.points, parms.target))
			{
				return false;
			}
		}
		else if (parms.questScriptDef != null && !parms.questScriptDef.CanRun(parms.points, parms.target))
		{
			return false;
		}
		if (Faction.OfPlayer.def.defName == "Milira_PlayerFaction")
		{
			return false;
		}
		return PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists_NoSuspended.Any();
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
