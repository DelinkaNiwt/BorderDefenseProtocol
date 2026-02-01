using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace GD3
{
	public class QuestPart_AddQuestMechModified : QuestPart_AddQuest
	{
		public List<Pawn> lodgers = new List<Pawn>();

		public FloatRange marketValueRange;

		public string inSignalRemovePawn;

		public QuestScriptDef def;

		public override QuestScriptDef QuestDef => def;

		public override Slate GetSlate()
		{
			Slate slate = new Slate();
			slate.Set("marketValueRange", marketValueRange);
			for (int i = 0; i < lodgers.Count; i++)
			{
				if (!lodgers[i].Dead && lodgers[i].Faction != Faction.OfPlayer && !lodgers[i].IsPrisoner)
				{
					slate.Set("rewardGiver", lodgers[i]);
					break;
				}
			}
			return slate;
		}

		public override void Notify_QuestSignalReceived(Signal signal)
		{
			base.Notify_QuestSignalReceived(signal);
			if (signal.tag == inSignalRemovePawn && signal.args.TryGetArg("SUBJECT", out Pawn arg) && lodgers.Contains(arg))
			{
				lodgers.Remove(arg);
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Collections.Look(ref lodgers, "lodgers", LookMode.Reference);
			Scribe_Values.Look(ref inSignalRemovePawn, "inSignalRemovePawn");
			Scribe_Values.Look(ref marketValueRange, "marketValueRange");
			Scribe_Defs.Look(ref def, "def");
			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				lodgers.RemoveAll((Pawn x) => x == null);
			}
		}
	}

}
