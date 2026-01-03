using System.Collections.Generic;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace Milira;

public class QuestNode_Root_FallenMiliraJoin_WalkIn : QuestNode_Root_WandererJoin
{
	private const int TimeoutTicks = 60000;

	public const float RelationWithColonistWeight = 0f;

	private string signalAccept;

	private string signalReject;

	protected override void RunInt()
	{
		Faction faction = Find.FactionManager.FirstFactionOfDef(MiliraDefOf.Milira_Faction);
		if (faction == null || faction.RelationKindWith(Faction.OfPlayer) == FactionRelationKind.Hostile)
		{
			return;
		}
		Pawn pawn = Current.Game.GetComponent<MiliraGameComponent_OverallControl>().pawn;
		if (pawn != null || !(pawn.Faction.def.defName != "Milira_Faction"))
		{
			base.RunInt();
			Quest quest = QuestGen.quest;
			quest.Delay(60000, delegate
			{
				QuestGen_End.End(quest, QuestEndOutcome.Fail);
			});
		}
	}

	public override Pawn GeneratePawn()
	{
		return Current.Game.GetComponent<MiliraGameComponent_OverallControl>().pawn;
	}

	protected override void AddSpawnPawnQuestParts(Quest quest, Map map, Pawn pawn)
	{
		signalAccept = QuestGenUtility.HardcodedSignalWithQuestID("Accept");
		signalReject = QuestGenUtility.HardcodedSignalWithQuestID("Reject");
		quest.Signal(signalAccept, delegate
		{
			quest.SetFaction(Gen.YieldSingle(pawn), Faction.OfPlayer);
			quest.PawnsArrive(Gen.YieldSingle(pawn), null, map.Parent);
			List<Pawn> list = new List<Pawn> { pawn };
			QuestGen_End.End(quest, QuestEndOutcome.Success);
			Current.Game.GetComponent<MiliraGameComponent_OverallControl>().pawn = null;
			Current.Game.GetComponent<MiliraGameComponent_OverallControl>().pawnInColony = pawn;
		});
		quest.Signal(signalReject, delegate
		{
			QuestGen_End.End(quest, QuestEndOutcome.Fail);
			Current.Game.GetComponent<MiliraGameComponent_OverallControl>().pawn = null;
		});
	}

	public override void SendLetter(Quest quest, Pawn pawn)
	{
		TaggedString title = "Milira.LetterFallenAngleJoin".Translate(pawn);
		TaggedString text = "Milira.LetterFallenAngleJoinDesc".Translate(pawn);
		PawnRelationUtility.TryAppendRelationsWithColonistsInfo(ref text, ref title, pawn);
		ChoiceLetter_AcceptJoiner choiceLetter_AcceptJoiner = (ChoiceLetter_AcceptJoiner)LetterMaker.MakeLetter(title, text, LetterDefOf.AcceptJoiner);
		choiceLetter_AcceptJoiner.signalAccept = signalAccept;
		choiceLetter_AcceptJoiner.signalReject = signalReject;
		choiceLetter_AcceptJoiner.quest = quest;
		choiceLetter_AcceptJoiner.StartTimeout(60000);
		Find.LetterStack.ReceiveLetter(choiceLetter_AcceptJoiner);
	}
}
