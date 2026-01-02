using RimWorld;
using Verse;

namespace AncotLibrary;

public class QuestPart_IncidentQueue : QuestPart
{
	public string inSignal;

	public IncidentDef incidentDef;

	public Faction faction;

	public Map map;

	public int incidentDelayTicks;

	public float points;

	public override void Notify_QuestSignalReceived(Signal signal)
	{
		base.Notify_QuestSignalReceived(signal);
		if (signal.tag == inSignal)
		{
			IncidentParms incidentParms = new IncidentParms();
			incidentParms.target = map;
			incidentParms.faction = faction;
			incidentParms.points = StorytellerUtility.DefaultThreatPointsNow(map);
			Find.Storyteller.incidentQueue.Add(incidentDef, Find.TickManager.TicksGame + incidentDelayTicks, incidentParms, 600000);
		}
	}
}
