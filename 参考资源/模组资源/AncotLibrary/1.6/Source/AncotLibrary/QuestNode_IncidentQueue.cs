using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace AncotLibrary;

public class QuestNode_IncidentQueue : QuestNode
{
	public SlateRef<IncidentDef> incidentDef;

	public SlateRef<Faction> faction;

	[NoTranslate]
	public SlateRef<string> inSignal;

	public SlateRef<int> incidentDelayTicks = 0;

	public int points;

	protected override bool TestRunInt(Slate slate)
	{
		return true;
	}

	protected override void RunInt()
	{
		Slate slate = QuestGen.slate;
		QuestPart_IncidentQueue questPart_IncidentQueue = new QuestPart_IncidentQueue();
		questPart_IncidentQueue.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(inSignal.GetValue(slate)) ?? slate.Get<string>("inSignal");
		questPart_IncidentQueue.incidentDef = incidentDef.GetValue(slate);
		questPart_IncidentQueue.faction = faction.GetValue(slate);
		questPart_IncidentQueue.incidentDelayTicks = incidentDelayTicks.GetValue(slate);
		questPart_IncidentQueue.points = slate.Get("points", 0f);
		questPart_IncidentQueue.map = slate.Get<Map>("map");
		QuestGen.quest.AddPart(questPart_IncidentQueue);
	}
}
