using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace AncotLibrary;

public class QuestPart_LordJob_ExitMapBest : QuestPart
{
	public List<Pawn> pawns;

	public Map map;

	public Faction faction;

	public string inSignal;

	private Lord lord;

	public override void Notify_QuestSignalReceived(Signal signal)
	{
		base.Notify_QuestSignalReceived(signal);
		if (!(signal.tag == inSignal))
		{
			return;
		}
		List<Pawn> list = new List<Pawn>();
		foreach (Pawn pawn in pawns)
		{
			if (pawn != null && pawn.Spawned && pawn.Map == map)
			{
				pawn.SetFaction(faction);
				list.Add(pawn);
			}
		}
		if (list.Any())
		{
			lord = LordMaker.MakeNewLord(faction ?? Faction.OfPlayer, new LordJob_ExitMapBest(LocomotionUrgency.Walk, canDig: false, canDefendSelf: true), map, list);
		}
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Collections.Look(ref pawns, "pawns", LookMode.Reference);
		Scribe_References.Look(ref map, "map");
		Scribe_References.Look(ref faction, "faction");
		Scribe_Values.Look(ref inSignal, "inSignal");
	}

	public override void AssignDebugData()
	{
		base.AssignDebugData();
		inSignal = "DebugSignal" + Rand.Int;
	}
}
