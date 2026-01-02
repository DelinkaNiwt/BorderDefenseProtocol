using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace AncotLibrary;

public class QuestPart_GetSpecificPawnInMap : QuestPart
{
	public string inSignal;

	[NoTranslate]
	public SlateRef<Pawn> specificPawn;

	public Map map;

	public Faction faction;

	public QuestNode node;

	public QuestNode elseNode;

	public override void Notify_QuestSignalReceived(Signal signal)
	{
		base.Notify_QuestSignalReceived(signal);
		Slate slate = QuestGen.slate;
		Pawn value = specificPawn.GetValue(slate);
		if (!(signal.tag == inSignal))
		{
			return;
		}
		if (value == null)
		{
			Log.Message("pawn == null");
		}
		if (map == null)
		{
			Log.Message("map == null");
		}
		if (value != null && map != null && value.Map != null)
		{
			if (node != null)
			{
				node.Run();
			}
		}
		else if (elseNode != null)
		{
			elseNode.Run();
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
