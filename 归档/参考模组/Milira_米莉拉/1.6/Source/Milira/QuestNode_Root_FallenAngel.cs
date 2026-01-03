using System.Collections.Generic;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace Milira;

public class QuestNode_Root_FallenAngel : QuestNode_Root_WandererJoin
{
	[NoTranslate]
	public SlateRef<string> storeAs;

	protected override bool TestRunInt(Slate slate)
	{
		return Current.Game.GetComponent<MiliraGameComponent_OverallControl>().canSendFallenMilira;
	}

	public override Pawn GeneratePawn()
	{
		Faction faction = Find.FactionManager.FirstFactionOfDef(MiliraDefOf.Milira_Faction);
		PawnGenerationRequest request = new PawnGenerationRequest(MiliraDefOf.Milira_FallenAngel, faction, PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: true, allowDead: false, allowDowned: true, canGeneratePawnRelations: true, mustBeCapableOfViolence: false, 1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: false, allowFood: true, allowAddictions: true, inhabitant: false, certainlyBeenInCryptosleep: false, forceRedressWorldPawnIfFormerColonist: false, worldPawnFactionDoesntMatter: false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, forceNoIdeo: true, forceNoBackstory: false, forbidAnyTitle: false, forceDead: false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null, forceRecruitable: true);
		Pawn pawn = PawnGenerator.GeneratePawn(request);
		pawn.guest.Recruitable = true;
		HealthUtility.DamageUntilDowned(pawn);
		float sevOffset = Rand.Range(0.1f, 0.2f);
		HealthUtility.AdjustSeverity(pawn, MiliraDefOf.Abasia, sevOffset);
		Current.Game.GetComponent<MiliraGameComponent_OverallControl>().pawn = pawn;
		Current.Game.GetComponent<MiliraGameComponent_OverallControl>().canSendChurchFirstTime = true;
		Current.Game.GetComponent<MiliraGameComponent_OverallControl>().canSendFallenMilira = false;
		return pawn;
	}

	protected override void AddSpawnPawnQuestParts(Quest quest, Map map, Pawn pawn)
	{
		List<Thing> contents = new List<Thing> { pawn };
		Faction faction = Find.FactionManager.FirstFactionOfDef(MiliraDefOf.Milira_Faction);
		quest.DropPods(map.Parent, contents, null, null, null, null, false, useTradeDropSpot: false, joinPlayer: false, makePrisoners: false, null, null, QuestPart.SignalListenMode.OngoingOnly, null, destroyItemsOnCleanup: true, dropAllInSamePod: true, allowFogged: false, canRetargetAnyMap: false, faction);
	}

	public override void SendLetter(Quest quest, Pawn pawn)
	{
		TaggedString title = "Milira.LetterFallenAngel".Translate();
		TaggedString letterText = "Milira.LetterFallenAngelDesc".Translate(pawn.Named("PAWN")).AdjustedFor(pawn);
		letterText += "\n\n";
		if (pawn.Faction == null)
		{
			letterText += "RefugeePodCrash_Factionless".Translate(pawn.Named("PAWN")).AdjustedFor(pawn);
		}
		else if (pawn.Faction.HostileTo(Faction.OfPlayer))
		{
			letterText += "RefugeePodCrash_Hostile".Translate(pawn.Named("PAWN")).AdjustedFor(pawn);
		}
		else
		{
			letterText += "RefugeePodCrash_NonHostile".Translate(pawn.Named("PAWN")).AdjustedFor(pawn);
		}
		if (pawn.DevelopmentalStage.Juvenile())
		{
			string arg = (pawn.ageTracker.AgeBiologicalYears * 3600000).ToStringTicksToPeriod();
			letterText += "\n\n" + "RefugeePodCrash_Child".Translate(pawn.Named("PAWN"), arg.Named("AGE"));
		}
		QuestNode_Root_WandererJoin_WalkIn.AppendCharityInfoToLetter("JoinerCharityInfo".Translate(pawn), ref letterText);
		PawnRelationUtility.TryAppendRelationsWithColonistsInfo(ref letterText, ref title, pawn);
		Find.LetterStack.ReceiveLetter(title, letterText, LetterDefOf.NeutralEvent, new TargetInfo(pawn));
	}
}
