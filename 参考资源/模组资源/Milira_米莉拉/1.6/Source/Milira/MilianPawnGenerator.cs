using RimWorld;
using Verse;

namespace Milira;

public class MilianPawnGenerator
{
	public static PawnKindDef PawnKindGenerator()
	{
		PawnKindDef milira_FallenAngel = MiliraDefOf.Milira_FallenAngel;
		milira_FallenAngel.race = MiliraDefOf.Milian_Race;
		milira_FallenAngel.defaultFactionDef = MiliraDefOf.Milira_Faction;
		milira_FallenAngel.backstoryCryptosleepCommonality = 0f;
		milira_FallenAngel.invNutrition = 0f;
		milira_FallenAngel.gearHealthRange = new FloatRange(0.8f, 1f);
		milira_FallenAngel.apparelIgnoreSeasons = true;
		milira_FallenAngel.maxGenerationAge = 1000;
		milira_FallenAngel.minGenerationAge = 0;
		milira_FallenAngel.initialWillRange = new FloatRange(0.8f, 1f);
		milira_FallenAngel.initialResistanceRange = new FloatRange(0.8f, 1f);
		milira_FallenAngel.styleItemTags = null;
		milira_FallenAngel.label = "Milian_PawnTest";
		milira_FallenAngel.combatPower = 100f;
		milira_FallenAngel.itemQuality = QualityCategory.Normal;
		milira_FallenAngel.isFighter = true;
		milira_FallenAngel.factionLeader = false;
		milira_FallenAngel.apparelMoney = new FloatRange(0f, 0f);
		milira_FallenAngel.apparelTags = null;
		milira_FallenAngel.apparelAllowHeadgearChance = 0f;
		milira_FallenAngel.weaponTags = null;
		milira_FallenAngel.backstoryCategories = null;
		milira_FallenAngel.abilities = null;
		return milira_FallenAngel;
	}

	public static Pawn GeneratePawn()
	{
		Faction faction = Find.FactionManager.FirstFactionOfDef(MiliraDefOf.Milira_Faction);
		PawnGenerationRequest request = new PawnGenerationRequest(PawnKindGenerator(), faction, PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: true, allowDead: false, allowDowned: true, canGeneratePawnRelations: true, mustBeCapableOfViolence: false, 1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: false, allowFood: true, allowAddictions: true, inhabitant: false, certainlyBeenInCryptosleep: false, forceRedressWorldPawnIfFormerColonist: false, worldPawnFactionDoesntMatter: false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, forceNoIdeo: true, forceNoBackstory: false, forbidAnyTitle: false, forceDead: false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null, forceRecruitable: true);
		return PawnGenerator.GeneratePawn(request);
	}
}
