using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI.Group;

namespace RimWorld.QuestGen;

public class TW_QuestNode_Root_Bossgroup : QuestNode
{
	private static readonly IntRange MaxDelayTicksRange = new IntRange(60000, 180000);

	private static readonly IntRange MinDelayTicksRange = new IntRange(2500, 5000);

	protected override void RunInt()
	{
		Slate slate = QuestGen.slate;
		Quest quest = QuestGen.quest;
		Map map = slate.Get<Map>("map");
		ThingDef thingDef = slate.Get<ThingDef>("reward");
		BossgroupDef bossgroupDef = slate.Get<BossgroupDef>("bossgroup");
		int timesSummoned = slate.Get("wave", 0);
		Faction faction = Faction.OfMechanoids;
		if (faction == null)
		{
			List<FactionRelation> list = new List<FactionRelation>();
			foreach (Faction other in Find.FactionManager.AllFactionsListForReading)
			{
				list.Add(new FactionRelation
				{
					other = other,
					kind = FactionRelationKind.Hostile
				});
			}
			faction = FactionGenerator.NewGeneratedFactionWithRelations(new FactionGeneratorParms(FactionDefOf.Mechanoid, default(IdeoGenerationParms), true), list);
			faction.temporary = true;
			Find.FactionManager.Add(faction);
		}
		List<Pawn> escorts = new List<Pawn>();
		List<Pawn> bosses = new List<Pawn>();
		int waveIndex = bossgroupDef.GetWaveIndex(timesSummoned);
		BossGroupWave wave = bossgroupDef.GetWave(waveIndex);
		PawnGenerationRequest request = new PawnGenerationRequest(bossgroupDef.boss.kindDef, faction, PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: true);
		for (int i = 0; i < wave.bossCount; i++)
		{
			Pawn boss = PawnGenerator.GeneratePawn(request);
			if (!wave.bossApparel.NullOrEmpty())
			{
				for (int j = 0; j < wave.bossApparel.Count; j++)
				{
					Apparel newApparel = (Apparel)ThingMaker.MakeThing(wave.bossApparel[j]);
					boss.apparel.Wear(newApparel, dropReplacedApparel: true, locked: true);
				}
			}
			Find.WorldPawns.PassToWorld(boss);
			bosses.Add(boss);
		}
		for (int k = 0; k < wave.escorts.Count; k++)
		{
			PawnGenerationRequest request2 = new PawnGenerationRequest(wave.escorts[k].kindDef, faction, PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: true);
			for (int l = 0; l < wave.escorts[k].count; l++)
			{
				Pawn escort = PawnGenerator.GeneratePawn(request2);
				escorts.Add(escort);
				Find.WorldPawns.PassToWorld(escort);
			}
		}
		slate.Set("mapParent", map.Parent);
		slate.Set("escortees", bosses.ToList());
		IntVec3 escortDropSpot = DropCellFinder.FindRaidDropCenterDistant(map);
		IntVec3 bossDropSpot = FindOppositeDropSpot(map, escortDropSpot);
		IEnumerable<Pawn> allPawns = bosses.Concat(escorts);
		foreach (Pawn pawn in allPawns)
		{
			map.attackTargetsCache.UpdateTarget(pawn);
		}
		string arrivalSignal = QuestGen.GenerateNewSignal("BossgroupArrives");
		QuestPart_BossgroupArrives questPart_BossgroupArrives = new QuestPart_BossgroupArrives();
		questPart_BossgroupArrives.mapParent = map.Parent;
		questPart_BossgroupArrives.bossgroupDef = bossgroupDef;
		questPart_BossgroupArrives.minDelay = MinDelayTicksRange.RandomInRange;
		questPart_BossgroupArrives.maxDelay = MaxDelayTicksRange.RandomInRange;
		questPart_BossgroupArrives.inSignalEnable = QuestGen.slate.Get<string>("inSignal");
		questPart_BossgroupArrives.outSignalsCompleted.Add(arrivalSignal);
		quest.AddPart(questPart_BossgroupArrives);
		quest.DropPods(map.Parent, bosses, null, null, null, null, false, useTradeDropSpot: false, joinPlayer: false, makePrisoners: false, arrivalSignal, null, QuestPart.SignalListenMode.OngoingOnly, bossDropSpot, destroyItemsOnCleanup: true, dropAllInSamePod: false, allowFogged: false, canRetargetAnyMap: false, Faction.OfMechanoids);
		quest.DropPods(map.Parent, escorts, null, null, null, null, false, useTradeDropSpot: false, joinPlayer: false, makePrisoners: false, arrivalSignal, null, QuestPart.SignalListenMode.OngoingOnly, escortDropSpot, destroyItemsOnCleanup: true, dropAllInSamePod: false, allowFogged: false, canRetargetAnyMap: false, Faction.OfMechanoids);
		LordJob lordJob = new LordJob_AssaultColony(faction, canKidnap: true, canTimeoutOrFlee: false);
		LordMaker.MakeNewLord(faction, lordJob, map, allPawns);
		Quest quest2 = quest;
		LetterDef neutralEvent = LetterDefOf.NeutralEvent;
		string inSignal = null;
		string chosenPawnSignal = null;
		string label = "LetterLabelBossgroupSummoned".Translate(bossgroupDef.boss.kindDef.LabelCap);
		string text = "LetterBossgroupSummoned".Translate(faction.NameColored.ToString()).ToString();
		quest2.Letter(neutralEvent, inSignal, chosenPawnSignal, Faction.OfMechanoids, null, useColonistsFromCaravanArg: false, QuestPart.SignalListenMode.OngoingOnly, null, filterDeadPawnsFromLookTargets: false, text, null, label);
		Quest quest3 = quest;
		LetterDef bossgroup = LetterDefOf.Bossgroup;
		label = "LetterLabelBossgroupArrived".Translate(bossgroupDef.boss.kindDef.LabelCap);
		inSignal = arrivalSignal;
		chosenPawnSignal = null;
		text = "LetterBossgroupArrived".Translate(faction.NameColored.ToString(), bossgroupDef.LeaderDescription, bossgroupDef.boss.kindDef.label, faction.def.pawnsPlural, bossgroupDef.GetWaveDescription(waveIndex)).ToString();
		quest3.Letter(bossgroup, inSignal, chosenPawnSignal, Faction.OfMechanoids, null, useColonistsFromCaravanArg: false, QuestPart.SignalListenMode.OngoingOnly, allPawns, filterDeadPawnsFromLookTargets: false, text, null, label);
		QuestPart_Bossgroup questPart_Bossgroup = new QuestPart_Bossgroup();
		questPart_Bossgroup.pawns.AddRange(allPawns);
		questPart_Bossgroup.faction = Faction.OfMechanoids;
		questPart_Bossgroup.mapParent = map.Parent;
		questPart_Bossgroup.bosses.AddRange(bosses);
		questPart_Bossgroup.stageLocation = escortDropSpot;
		questPart_Bossgroup.inSignal = arrivalSignal;
		quest.AddPart(questPart_Bossgroup);
		quest.Alert("AlertBossgroupIncoming".Translate(bossgroupDef.boss.kindDef.LabelCap), "AlertBossgroupIncomingDesc".Translate(bossgroupDef.boss.kindDef.label), null, critical: true, getLookTargetsFromSignal: false, null, arrivalSignal);
		string inSignal3 = QuestGenUtility.HardcodedSignalWithQuestID("escortees.KilledLeavingsLeft");
		quest.ThingAnalyzed(thingDef, delegate
		{
			quest.Letter(LetterDefOf.PositiveEvent, null, null, null, null, useColonistsFromCaravanArg: false, QuestPart.SignalListenMode.OngoingOnly, null, filterDeadPawnsFromLookTargets: false, "[bossDefeatedLetterText]", null, "[bossDefeatedLetterLabel]");
		}, delegate
		{
			quest.Letter(LetterDefOf.PositiveEvent, null, null, null, null, useColonistsFromCaravanArg: false, QuestPart.SignalListenMode.OngoingOnly, null, filterDeadPawnsFromLookTargets: false, "[bossDefeatedStudyChipLetterText]", null, "[bossDefeatedLetterLabel]");
		}, inSignal3);
		quest.AnyPawnAlive(bosses, null, delegate
		{
			QuestGen_End.End(quest, QuestEndOutcome.Unknown);
		}, QuestGenUtility.HardcodedSignalWithQuestID("escortees.Killed"));
		quest.End(QuestEndOutcome.Unknown, 0, null, QuestGenUtility.HardcodedSignalWithQuestID("mapParent.Destroyed"));
	}

	private IntVec3 FindOppositeDropSpot(Map map, IntVec3 referenceSpot)
	{
		IntVec3 oppositeSpot = new IntVec3(map.Size.x - referenceSpot.x, 0, map.Size.z - referenceSpot.z);
		if (!DropCellFinder.IsGoodDropSpot(oppositeSpot, map, allowFogged: true, canRoofPunch: true) && !CellFinder.TryFindRandomEdgeCellWith((IntVec3 c) => DropCellFinder.IsGoodDropSpot(c, map, allowFogged: true, canRoofPunch: true), map, CellFinder.EdgeRoadChance_Neutral, out oppositeSpot))
		{
			return referenceSpot;
		}
		return oppositeSpot;
	}

	protected override bool TestRunInt(Slate slate)
	{
		return slate.Exists("wave") && slate.Exists("bossgroup") && slate.Exists("map") && slate.Exists("reward");
	}
}
