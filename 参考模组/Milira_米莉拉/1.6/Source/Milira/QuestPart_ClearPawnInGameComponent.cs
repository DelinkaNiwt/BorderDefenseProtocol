using RimWorld;
using Verse;

namespace Milira;

public class QuestPart_ClearPawnInGameComponent : QuestPart
{
	public string inSignal;

	public int miliraThreatPoint;

	public override void Notify_QuestSignalReceived(Signal signal)
	{
		base.Notify_QuestSignalReceived(signal);
		if (signal.tag == inSignal)
		{
			Current.Game.GetComponent<MiliraGameComponent_OverallControl>().pawn = null;
		}
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref inSignal, "inSignal");
	}

	public override void AssignDebugData()
	{
		base.AssignDebugData();
		inSignal = "DebugSignal" + Rand.Int;
	}
}
