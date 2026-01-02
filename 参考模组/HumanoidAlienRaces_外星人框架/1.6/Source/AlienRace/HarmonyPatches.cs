using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using AlienRace.ApparelGraphics;
using AlienRace.ExtendedGraphics;
using HarmonyLib;
using LudeonTK;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Grammar;

namespace AlienRace;

[StaticConstructorOnStartup]
public static class HarmonyPatches
{
	private static readonly Type patchType;

	private static Comp_OutfitStandHAR outfitStandComp;

	public static PawnGenerationRequest currentStartingRequest;

	public static bool firstStartingRequest;

	public static int currentBirthCount;

	public static Pawn growthMomentPawn;

	public static Pawn createPawnAtlasPawn;

	private static BodyPartDef headPawnDef;

	private static HashSet<ThingStuffPair> apparelList;

	private static readonly HashSet<ThingStuffPair> weaponList;

	public static int ingestedCount;

	public static HashSet<ThingDef> colonistRaces;

	private static int colonistRacesTick;

	private const int COLONIST_RACES_TICK_TIMER = 5000;

	private static PawnKindDef startingPawnKindRestriction;

	private static string startingPawnKindLabel;

	private static PawnBioDef bioReference;

	static HarmonyPatches()
	{
		patchType = typeof(HarmonyPatches);
		currentBirthCount = int.MinValue;
		weaponList = new HashSet<ThingStuffPair>();
		colonistRaces = new HashSet<ThingDef>();
		AlienHarmony harmony = new AlienHarmony("rimworld.erdelf.alien_race.main");
		harmony.Patch(AccessTools.Method(typeof(PawnRelationWorker_Child), "GenerationChance"), null, new HarmonyMethod(patchType, "GenerationChanceChildPostfix"));
		harmony.Patch(AccessTools.Method(typeof(PawnRelationWorker_ExLover), "GenerationChance"), null, new HarmonyMethod(patchType, "GenerationChanceExLoverPostfix"));
		harmony.Patch(AccessTools.Method(typeof(PawnRelationWorker_ExSpouse), "GenerationChance"), null, new HarmonyMethod(patchType, "GenerationChanceExSpousePostfix"));
		harmony.Patch(AccessTools.Method(typeof(PawnRelationWorker_Fiance), "GenerationChance"), null, new HarmonyMethod(patchType, "GenerationChanceFiancePostfix"));
		harmony.Patch(AccessTools.Method(typeof(PawnRelationWorker_Lover), "GenerationChance"), null, new HarmonyMethod(patchType, "GenerationChanceLoverPostfix"));
		harmony.Patch(AccessTools.Method(typeof(PawnRelationWorker_Parent), "GenerationChance"), null, new HarmonyMethod(patchType, "GenerationChanceParentPostfix"));
		harmony.Patch(AccessTools.Method(typeof(PawnRelationWorker_Sibling), "GenerationChance"), null, new HarmonyMethod(patchType, "GenerationChanceSiblingPostfix"));
		harmony.Patch(AccessTools.Method(typeof(PawnRelationWorker_Spouse), "GenerationChance"), null, new HarmonyMethod(patchType, "GenerationChanceSpousePostfix"));
		harmony.Patch(AccessTools.Method(typeof(PawnGenerator), "GeneratePawnRelations"), new HarmonyMethod(patchType, "GeneratePawnRelationsPrefix"));
		harmony.Patch(AccessTools.Method(typeof(PawnRelationDef), "GetGenderSpecificLabel"), new HarmonyMethod(patchType, "GetGenderSpecificLabelPrefix"));
		harmony.Patch(AccessTools.Method(typeof(PawnBioAndNameGenerator), "TryGetRandomUnusedSolidBioFor"), null, new HarmonyMethod(patchType, "TryGetRandomUnusedSolidBioForPostfix"));
		harmony.Patch(AccessTools.Method(typeof(PawnBioAndNameGenerator), "FillBackstorySlotShuffled"), new HarmonyMethod(patchType, "FillBackstoryInSlotShuffledPrefix"), null, new HarmonyMethod(patchType, "FillBackstorySlotShuffledTranspiler"));
		harmony.Patch(AccessTools.Method(typeof(WorkGiver_Researcher), "ShouldSkip"), null, new HarmonyMethod(patchType, "ShouldSkipResearchPostfix"));
		harmony.Patch(AccessTools.PropertyGetter(typeof(MainTabWindow_Research), "VisibleResearchProjects"), null, null, new HarmonyMethod(patchType, "ResearchScreenTranspiler"));
		harmony.Patch(AccessTools.Method(typeof(GenConstruct), "CanConstruct", new Type[5]
		{
			typeof(Thing),
			typeof(Pawn),
			typeof(bool),
			typeof(bool),
			typeof(JobDef)
		}), null, new HarmonyMethod(patchType, "CanConstructPostfix"));
		harmony.Patch(AccessTools.Method(typeof(GameRules), "DesignatorAllowed"), null, null, new HarmonyMethod(patchType, "DesignatorAllowedTranspiler"));
		harmony.Patch(AccessTools.Method(typeof(Bill), "PawnAllowedToStartAnew"), null, new HarmonyMethod(patchType, "PawnAllowedToStartAnewPostfix"));
		harmony.Patch(AccessTools.Method(typeof(WorkGiver_GrowerHarvest), "HasJobOnCell"), null, new HarmonyMethod(patchType, "HasJobOnCellHarvestPostfix"));
		harmony.Patch(AccessTools.Method(typeof(WorkGiver_GrowerSow), "ExtraRequirements"), null, new HarmonyMethod(patchType, "ExtraRequirementsGrowerSowPostfix"));
		harmony.Patch(AccessTools.Method(typeof(Pawn), "SetFaction"), null, new HarmonyMethod(patchType, "SetFactionPostfix"));
		harmony.Patch(AccessTools.Method(typeof(Thing), "SetFactionDirect"), null, new HarmonyMethod(patchType, "SetFactionDirectPostfix"));
		harmony.Patch(AccessTools.Method(typeof(JobGiver_OptimizeApparel), "ApparelScoreGain"), null, new HarmonyMethod(patchType, "ApparelScoreGainPostFix"));
		harmony.Patch(AccessTools.Method(typeof(ThoughtUtility), "CanGetThought"), null, new HarmonyMethod(patchType, "CanGetThoughtPostfix"));
		harmony.Patch(AccessTools.Method(typeof(FoodUtility), "ThoughtsFromIngesting"), null, new HarmonyMethod(patchType, "ThoughtsFromIngestingPostfix"));
		harmony.Patch(AccessTools.Method(typeof(MemoryThoughtHandler), "TryGainMemory", new Type[2]
		{
			typeof(Thought_Memory),
			typeof(Pawn)
		}), new HarmonyMethod(patchType, "TryGainMemoryPrefix"));
		harmony.Patch(AccessTools.Method(typeof(SituationalThoughtHandler), "TryCreateThought"), new HarmonyMethod(patchType, "TryCreateThoughtPrefix"));
		harmony.Patch(AccessTools.Method(typeof(MemoryThoughtHandler), "RemoveMemoriesOfDef"), new HarmonyMethod(patchType, "ThoughtReplacementPrefix"));
		harmony.Patch(AccessTools.Method(typeof(MemoryThoughtHandler), "RemoveMemoriesOfDefIf"), new HarmonyMethod(patchType, "ThoughtReplacementPrefix"));
		harmony.Patch(AccessTools.Method(typeof(MemoryThoughtHandler), "RemoveMemoriesOfDefWhereOtherPawnIs"), new HarmonyMethod(patchType, "ThoughtReplacementPrefix"));
		harmony.Patch(AccessTools.Method(typeof(MemoryThoughtHandler), "OldestMemoryOfDef"), new HarmonyMethod(patchType, "ThoughtReplacementPrefix"));
		harmony.Patch(AccessTools.Method(typeof(MemoryThoughtHandler), "NumMemoriesOfDef"), new HarmonyMethod(patchType, "ThoughtReplacementPrefix"));
		harmony.Patch(AccessTools.Method(typeof(MemoryThoughtHandler), "GetFirstMemoryOfDef"), new HarmonyMethod(patchType, "ThoughtReplacementPrefix"));
		harmony.Patch(AccessTools.Method(typeof(AgeInjuryUtility), "GenerateRandomOldAgeInjuries"), new HarmonyMethod(patchType, "GenerateRandomOldAgeInjuriesPrefix"));
		harmony.Patch(AccessTools.Method(typeof(AgeInjuryUtility), "RandomHediffsToGainOnBirthday", new Type[3]
		{
			typeof(ThingDef),
			typeof(float),
			typeof(float)
		}), null, new HarmonyMethod(patchType, "RandomHediffsToGainOnBirthdayPostfix"));
		harmony.Patch(AccessTools.Method(typeof(PawnGenerator), "GenerateRandomAge"), new HarmonyMethod(patchType, "GenerateRandomAgePrefix"));
		harmony.Patch(AccessTools.Method(typeof(PawnGenerator), "GenerateTraits"), new HarmonyMethod(patchType, "GenerateTraitsPrefix"), new HarmonyMethod(patchType, "GenerateTraitsPostfix"));
		harmony.Patch(AccessTools.Method(typeof(PawnGenerator), "GenerateTraitsFor"), null, null, new HarmonyMethod(patchType, "GenerateTraitsForTranspiler"));
		harmony.Patch(AccessTools.Method(typeof(JobGiver_SatisfyChemicalNeed), "DrugValidator"), null, new HarmonyMethod(patchType, "DrugValidatorPostfix"));
		harmony.Patch(AccessTools.Method(typeof(CompDrug), "PostIngested"), null, new HarmonyMethod(patchType, "DrugPostIngestedPostfix"));
		harmony.Patch(AccessTools.Method(typeof(Thing), "Ingested"), null, null, new HarmonyMethod(patchType, "IngestedTranspiler"));
		harmony.Patch(AccessTools.Method(typeof(AddictionUtility), "CanBingeOnNow"), null, new HarmonyMethod(patchType, "CanBingeNowPostfix"));
		harmony.Patch(AccessTools.Method(typeof(PawnGenerator), "GenerateBodyType"), null, new HarmonyMethod(patchType, "GenerateBodyTypePostfix"));
		harmony.Patch(AccessTools.Method(typeof(PawnGenerator), "GetBodyTypeFor"), null, new HarmonyMethod(patchType, "GetBodyTypeForPostfix"));
		harmony.Patch(AccessTools.Property(typeof(Pawn_StoryTracker), "SkinColor").GetGetMethod(), null, null, new HarmonyMethod(patchType, "SkinColorTranspiler"));
		harmony.Patch(AccessTools.Method(typeof(Pawn_AgeTracker), "BirthdayBiological"), new HarmonyMethod(patchType, "BirthdayBiologicalPrefix"));
		harmony.Patch(AccessTools.Method(typeof(PawnGenerator), "GeneratePawn", new Type[1] { typeof(PawnGenerationRequest) }), new HarmonyMethod(patchType, "GeneratePawnPrefix"), new HarmonyMethod(patchType, "GeneratePawnPostfix"));
		harmony.Patch(AccessTools.PropertyGetter(typeof(StartingPawnUtility), "DefaultStartingPawnRequest"), null, null, new HarmonyMethod(patchType, "DefaultStartingPawnTranspiler"));
		harmony.Patch(AccessTools.Method(typeof(PawnGenerator), "GenerateGenes"), new HarmonyMethod(patchType, "GenerateGenesPrefix"), new HarmonyMethod(patchType, "GenerateGenesPostfix"), new HarmonyMethod(patchType, "GenerateGenesTranspiler"));
		harmony.Patch(AccessTools.Method(typeof(PawnHairColors), "RandomHairColor"), null, null, new HarmonyMethod(patchType, "GenerateGenesTranspiler"));
		harmony.Patch(AccessTools.Method(typeof(PawnBioAndNameGenerator), "GeneratePawnName"), new HarmonyMethod(patchType, "GeneratePawnNamePrefix"));
		harmony.Patch(AccessTools.Method(typeof(Page_ConfigureStartingPawns), "CanDoNext"), null, null, new HarmonyMethod(patchType, "CanDoNextStartPawnTranspiler"));
		harmony.Patch(AccessTools.Method(typeof(CharacterCardUtility), "DrawCharacterCard"), null, null, new HarmonyMethod(patchType, "DrawCharacterCardTranspiler"));
		harmony.Patch(AccessTools.Method(typeof(GameInitData), "PrepForMapGen"), new HarmonyMethod(patchType, "PrepForMapGenPrefix"));
		harmony.Patch(AccessTools.Method(typeof(Pawn_RelationsTracker), "SecondaryLovinChanceFactor"), null, new HarmonyMethod(patchType, "SecondaryLovinChanceFactorPostfix"), new HarmonyMethod(patchType, "SecondaryLovinChanceFactorTranspiler"));
		harmony.Patch(AccessTools.Method(typeof(Pawn_RelationsTracker), "CompatibilityWith"), null, new HarmonyMethod(patchType, "CompatibilityWithPostfix"));
		harmony.Patch(AccessTools.Method(typeof(Faction), "TryMakeInitialRelationsWith"), null, new HarmonyMethod(patchType, "TryMakeInitialRelationsWithPostfix"));
		harmony.Patch(AccessTools.Method(typeof(TraitSet), "GainTrait"), new HarmonyMethod(patchType, "GainTraitPrefix"));
		harmony.Patch(AccessTools.Method(typeof(TraderCaravanUtility), "GetTraderCaravanRole"), null, null, new HarmonyMethod(patchType, "GetTraderCaravanRoleTranspiler"));
		harmony.Patch(AccessTools.Method(typeof(RestUtility), "CanUseBedEver"), null, new HarmonyMethod(patchType, "CanUseBedEverPostfix"));
		harmony.Patch(AccessTools.Property(typeof(CompAssignableToPawn_Bed), "AssigningCandidates").GetGetMethod(), null, new HarmonyMethod(patchType, "AssigningCandidatesPostfix"));
		harmony.Patch(AccessTools.Method(typeof(GrammarUtility), "RulesForPawn", new Type[5]
		{
			typeof(string),
			typeof(Pawn),
			typeof(Dictionary<string, string>),
			typeof(bool),
			typeof(bool)
		}), null, new HarmonyMethod(patchType, "RulesForPawnPostfix"));
		harmony.Patch(AccessTools.Method(typeof(RaceProperties), "CanEverEat", new Type[1] { typeof(ThingDef) }), null, new HarmonyMethod(patchType, "CanEverEatPostfix"));
		harmony.Patch(AccessTools.Method(typeof(Verb_MeleeAttackDamage), "DamageInfosToApply"), null, new HarmonyMethod(patchType, "DamageInfosToApplyPostfix"));
		harmony.Patch(AccessTools.Method(typeof(PawnWeaponGenerator), "TryGenerateWeaponFor"), new HarmonyMethod(patchType, "TryGenerateWeaponForPrefix"), new HarmonyMethod(patchType, "TryGenerateWeaponForCleanup"), new HarmonyMethod(patchType, "TryGenerateWeaponForTranspiler"));
		harmony.Patch(AccessTools.Method(typeof(PawnApparelGenerator), "GenerateStartingApparelFor"), new HarmonyMethod(patchType, "GenerateStartingApparelForPrefix"), new HarmonyMethod(patchType, "GenerateStartingApparelForPostfix"));
		harmony.Patch(AccessTools.Method(typeof(PawnGenerator), "GenerateInitialHediffs"), null, new HarmonyMethod(patchType, "GenerateInitialHediffsPostfix"));
		harmony.Patch(typeof(HediffSet).GetNestedTypes(AccessTools.all).SelectMany(AccessTools.GetDeclaredMethods).First((MethodInfo methodInfo) => methodInfo.ReturnType == typeof(bool) && methodInfo.GetParameters().First().ParameterType == typeof(BodyPartRecord)), null, new HarmonyMethod(patchType, "HasHeadPostfix"));
		harmony.Patch(AccessTools.Property(typeof(HediffSet), "HasHead").GetGetMethod(), new HarmonyMethod(patchType, "HasHeadPrefix"));
		harmony.Patch(AccessTools.Method(typeof(Pawn_AgeTracker), "RecalculateLifeStageIndex"), null, null, new HarmonyMethod(patchType, "RecalculateLifeStageIndexTranspiler"));
		harmony.Patch(AccessTools.Method(typeof(Designator), "CanDesignateThing"), null, new HarmonyMethod(patchType, "CanDesignateThingTamePostfix"));
		harmony.Patch(AccessTools.Method(typeof(WorkGiver_InteractAnimal), "CanInteractWithAnimal", new Type[7]
		{
			typeof(Pawn),
			typeof(Pawn),
			typeof(string).MakeByRefType(),
			typeof(bool),
			typeof(bool),
			typeof(bool),
			typeof(bool)
		}), null, new HarmonyMethod(patchType, "CanInteractWithAnimalPostfix"));
		harmony.Patch(AccessTools.Method(typeof(PawnRenderer), "BaseHeadOffsetAt"), null, new HarmonyMethod(patchType, "BaseHeadOffsetAtPostfix"), new HarmonyMethod(patchType, "BaseHeadOffsetAtTranspiler"));
		harmony.Patch(AccessTools.Method(typeof(Pawn_HealthTracker), "AddHediff", new Type[4]
		{
			typeof(Hediff),
			typeof(BodyPartRecord),
			typeof(DamageInfo?),
			typeof(DamageWorker.DamageResult)
		}), null, new HarmonyMethod(patchType, "AddHediffPostfix"));
		harmony.Patch(AccessTools.Method(typeof(Pawn_HealthTracker), "RemoveHediff"), null, new HarmonyMethod(patchType, "RemoveHediffPostfix"));
		harmony.Patch(AccessTools.Method(typeof(Pawn_HealthTracker), "Notify_HediffChanged"), null, new HarmonyMethod(patchType, "HediffChangedPostfix"));
		harmony.Patch(AccessTools.Method(typeof(PawnGenerator), "GenerateGearFor"), null, new HarmonyMethod(patchType, "GenerateGearForPostfix"));
		harmony.Patch(AccessTools.Method(typeof(Pawn), "ChangeKind"), new HarmonyMethod(patchType, "ChangeKindPrefix"));
		harmony.Patch(AccessTools.Method(typeof(EditWindow_TweakValues), "DoWindowContents"), null, null, new HarmonyMethod(typeof(TweakValues), "TweakValuesTranspiler"));
		HarmonyMethod misandryMisogonyTranspiler = new HarmonyMethod(patchType, "MisandryMisogynyTranspiler");
		harmony.Patch(AccessTools.Method(typeof(ThoughtWorker_Woman), "CurrentSocialStateInternal"), null, null, misandryMisogonyTranspiler);
		harmony.Patch(AccessTools.Method(typeof(ThoughtWorker_Man), "CurrentSocialStateInternal"), null, null, misandryMisogonyTranspiler);
		harmony.Patch(AccessTools.Method(typeof(EquipmentUtility), "CanEquip", new Type[4]
		{
			typeof(Thing),
			typeof(Pawn),
			typeof(string).MakeByRefType(),
			typeof(bool)
		}), null, new HarmonyMethod(patchType, "CanEquipPostfix"));
		harmony.Patch(AccessTools.Method(typeof(PawnBioAndNameGenerator), "GiveShuffledBioTo"), null, null, new HarmonyMethod(patchType, "MinAgeForAdulthood"));
		harmony.Patch(AccessTools.Method(typeof(PawnBioAndNameGenerator), "TryGiveSolidBioTo"), null, null, new HarmonyMethod(patchType, "MinAgeForAdulthood"));
		harmony.Patch(AccessTools.Method(typeof(PawnDrawUtility), "FindAnchors"), null, new HarmonyMethod(patchType, "FindAnchorsPostfix"));
		harmony.Patch(AccessTools.Method(typeof(PawnDrawUtility), "CalcAnchorData"), null, new HarmonyMethod(patchType, "CalcAnchorDataPostfix"));
		harmony.Patch(AccessTools.Method(typeof(PawnWoundDrawer), "WriteCache"), null, null, new HarmonyMethod(patchType, "WoundWriteCacheTranspiler"));
		harmony.Patch(AccessTools.Method(typeof(PawnCacheRenderer), "RenderPawn"), new HarmonyMethod(patchType, "CacheRenderPawnPrefix"));
		harmony.Patch(AccessTools.Constructor(typeof(PawnTextureAtlas)), null, null, new HarmonyMethod(patchType, "PawnTextureAtlasConstructorTranspiler"));
		harmony.Patch(AccessTools.Method(typeof(PawnTextureAtlas), "TryGetFrameSet"), null, null, new HarmonyMethod(patchType, "PawnTextureAtlasGetFrameSetTranspiler"));
		harmony.Patch(typeof(PawnTextureAtlas).GetNestedTypes(AccessTools.all)[0].GetMethods(AccessTools.all).First((MethodInfo methodInfo) => methodInfo.GetParameters().Any()), null, null, new HarmonyMethod(patchType, "PawnTextureAtlasConstructorFuncTranspiler"));
		harmony.Patch(AccessTools.Method(typeof(GlobalTextureAtlasManager), "TryGetPawnFrameSet"), new HarmonyMethod(patchType, "GlobalTextureAtlasGetFrameSetPrefix"));
		harmony.Patch(AccessTools.Method(typeof(PawnStyleItemChooser), "WantsToUseStyle"), new HarmonyMethod(patchType, "WantsToUseStylePrefix"), new HarmonyMethod(patchType, "WantsToUseStylePostfix"));
		harmony.Patch(AccessTools.Method(typeof(PreceptComp_SelfTookMemoryThought), "Notify_MemberTookAction"), null, null, new HarmonyMethod(patchType, "SelfTookMemoryThoughtTranspiler"));
		harmony.Patch(AccessTools.Method(typeof(PreceptComp_KnowsMemoryThought), "Notify_MemberWitnessedAction"), null, null, new HarmonyMethod(patchType, "KnowsMemoryThoughtTranspiler"));
		harmony.Patch(AccessTools.Method(typeof(PawnStyleItemChooser), "TotalStyleItemLikelihood"), null, new HarmonyMethod(patchType, "TotalStyleItemLikelihoodPostfix"));
		harmony.Patch(AccessTools.Method(typeof(Thing), "Ingested"), new HarmonyMethod(patchType, "IngestedPrefix"));
		harmony.Patch(AccessTools.Method(typeof(InteractionWorker_RomanceAttempt), "Interacted"), null, null, new HarmonyMethod(patchType, "RomanceAttemptInteractTranspiler"));
		harmony.Patch(AccessTools.Method(typeof(InteractionWorker_RomanceAttempt), "SuccessChance"), null, new HarmonyMethod(patchType, "RomanceAttemptSuccessChancePostfix"));
		harmony.Patch(AccessTools.Method(typeof(BedUtility), "WillingToShareBed"), null, new HarmonyMethod(patchType, "WillingToShareBedPostfix"));
		harmony.Patch(AccessTools.Method(typeof(Tradeable_Pawn), "ResolveTrade"), null, null, new HarmonyMethod(patchType, "TradeablePawnResolveTranspiler"));
		harmony.Patch(AccessTools.Method(typeof(TradeUI), "DrawTradeableRow"), null, null, new HarmonyMethod(patchType, "DrawTradeableRowTranspiler"));
		harmony.Patch(AccessTools.Method(typeof(Pawn_MindState), "SetupLastHumanMeatTick"), new HarmonyMethod(patchType, "SetupLastHumanMeatTickPrefix"));
		harmony.Patch(AccessTools.Method(typeof(FoodUtility), "AddThoughtsFromIdeo"), new HarmonyMethod(patchType, "FoodUtilityAddThoughtsFromIdeoPrefix"));
		harmony.Patch(AccessTools.Method(typeof(PreceptComp_UnwillingToDo_Gendered), "MemberWillingToDo"), null, null, new HarmonyMethod(patchType, "UnwillingWillingToDoGenderedTranspiler"));
		harmony.Patch(AccessTools.Method(typeof(JobDriver_Lovin), "GenerateRandomMinTicksToNextLovin"), null, null, new HarmonyMethod(patchType, "GenerateRandomMinTicksToNextLovinTranspiler"));
		harmony.Patch(AccessTools.Method(typeof(PawnGenerator), "GenerateSkills"), new HarmonyMethod(patchType, "GenerateSkillsPrefix"), new HarmonyMethod(patchType, "GenerateSkillsPostfix"));
		harmony.Patch(AccessTools.Method(typeof(PawnGenerator), "TryGenerateNewPawnInternal"), new HarmonyMethod(patchType, "TryGenerateNewPawnInternalPrefix"), null, new HarmonyMethod(patchType, "TryGenerateNewPawnInternalTranspiler"));
		harmony.Patch(AccessTools.Method(typeof(Pawn_GeneTracker), "Notify_GenesChanged"), null, null, new HarmonyMethod(patchType, "NotifyGenesChangedTranspiler"));
		harmony.Patch(AccessTools.Method(typeof(GrowthUtility), "IsGrowthBirthday"), null, null, new HarmonyMethod(patchType, "IsGrowthBirthdayTranspiler"));
		harmony.Patch(AccessTools.Method(typeof(Pawn_AgeTracker), "TryChildGrowthMoment"), new HarmonyMethod(patchType, "TryChildGrowthMomentPrefix"));
		harmony.Patch(AccessTools.Method(typeof(Gizmo_GrowthTier), "GrowthTierTooltip"), new HarmonyMethod(patchType, "GrowthTierTooltipPrefix"));
		harmony.Patch(AccessTools.Method(typeof(Pawn_AgeTracker), "TrySimulateGrowthPoints"), null, null, new HarmonyMethod(patchType, "TrySimulateGrowthPointsTranspiler"));
		harmony.Patch(AccessTools.Method(typeof(ChoiceLetter_GrowthMoment), "CacheLetterText"), null, null, new HarmonyMethod(patchType, "GrowthMomentCacheLetterTextTranspiler"));
		harmony.Patch(AccessTools.Method(typeof(Pawn_StyleTracker), "FinalizeHairColor"), null, new HarmonyMethod(patchType, "FinalizeHairColorPostfix"));
		harmony.Patch(AccessTools.Method(typeof(Toils_StyleChange), "FinalizeLookChange"), null, new HarmonyMethod(patchType, "FinalizeLookChangePostfix"));
		harmony.Patch(AccessTools.Method(typeof(StatPart_FertilityByGenderAge), "AgeFactor"), null, null, new HarmonyMethod(patchType, "FertilityAgeFactorTranspiler"));
		harmony.Patch(AccessTools.Method(typeof(Pawn_GeneTracker), "AddGene", new Type[2]
		{
			typeof(Gene),
			typeof(bool)
		}), new HarmonyMethod(patchType, "AddGenePrefix"));
		harmony.Patch(AccessTools.Method(typeof(ApparelGraphicRecordGetter), "TryGetGraphicApparel"), null, null, new HarmonyMethod(patchType, "TryGetGraphicApparelTranspiler"));
		harmony.Patch(AccessTools.Method(typeof(PregnancyUtility), "PregnancyChanceForPartners"), new HarmonyMethod(patchType, "PregnancyChanceForPartnersPrefix"));
		harmony.Patch(AccessTools.Method(typeof(PregnancyUtility), "CanEverProduceChild"), null, new HarmonyMethod(patchType, "CanEverProduceChildPostfix"), new HarmonyMethod(patchType, "CanEverProduceChildTranspiler"));
		harmony.Patch(AccessTools.Method(typeof(Recipe_ExtractOvum), "AvailableReport"), null, new HarmonyMethod(patchType, "ExtractOvumAvailableReportPostfix"));
		harmony.Patch(AccessTools.Method(typeof(HumanOvum), "CanFertilizeReport"), null, new HarmonyMethod(patchType, "HumanOvumCanFertilizeReportPostfix"));
		harmony.Patch(AccessTools.Method(typeof(HumanEmbryo), "ImplantPawnValid"), new HarmonyMethod(patchType, "EmbryoImplantPawnPrefix"));
		harmony.Patch(AccessTools.Method(typeof(HumanEmbryo), "CanImplantReport"), null, new HarmonyMethod(patchType, "EmbryoImplantReportPostfix"));
		harmony.Patch(AccessTools.Method(typeof(LifeStageWorker_HumanlikeChild), "Notify_LifeStageStarted"), null, new HarmonyMethod(patchType, "ChildLifeStageStartedPostfix"), new HarmonyMethod(patchType, "ChildLifeStageStartedTranspiler"));
		harmony.Patch(AccessTools.Method(typeof(LifeStageWorker_HumanlikeAdult), "Notify_LifeStageStarted"), null, new HarmonyMethod(patchType, "AdultLifeStageStartedPostfix"), new HarmonyMethod(patchType, "AdultLifeStageStartedTranspiler"));
		harmony.Patch(AccessTools.Method(typeof(PawnBioAndNameGenerator), "GetBackstoryCategoryFiltersFor"), null, new HarmonyMethod(patchType, "GetBackstoryCategoryFiltersForPostfix"));
		harmony.Patch(AccessTools.Method(typeof(QuestNode_Root_WandererJoin_WalkIn), "GeneratePawn"), null, null, new HarmonyMethod(patchType, "WandererJoinTranspiler"));
		harmony.Patch(AccessTools.Method(typeof(PregnancyUtility), "ApplyBirthOutcome"), null, null, new HarmonyMethod(patchType, "ApplyBirthOutcomeTranspiler"));
		harmony.Patch(AccessTools.Method(typeof(PawnGenerator), "XenotypesAvailableFor"), null, new HarmonyMethod(patchType, "XenotypesAvailableForPostfix"));
		harmony.Patch(AccessTools.Method(typeof(PawnGenerator), "GetXenotypeForGeneratedPawn"), null, null, new HarmonyMethod(patchType, "GetXenotypeForGeneratedPawnTranspiler"));
		harmony.Patch(AccessTools.Method(typeof(Pawn_GeneTracker), "SetXenotype"), new HarmonyMethod(patchType, "SetXenotypePrefix"));
		harmony.Patch(AccessTools.Method(typeof(CharacterCardUtility), "LifestageAndXenotypeOptions"), null, null, new HarmonyMethod(patchType, "LifestageAndXenotypeOptionsTranspiler"));
		harmony.Patch(AccessTools.Method(typeof(PawnGenerator), "AdjustXenotypeForFactionlessPawn"), null, null, new HarmonyMethod(patchType, "LifestageAndXenotypeOptionsTranspiler"));
		harmony.Patch(AccessTools.Method(typeof(StartingPawnUtility), "NewGeneratedStartingPawn"), null, null, new HarmonyMethod(patchType, "NewGeneratedStartingPawnTranspiler"));
		harmony.Patch(AccessTools.Method(typeof(PawnHairColors), "HasGreyHair"), null, null, new HarmonyMethod(patchType, "HasGreyHairTranspiler"));
		harmony.Patch(AccessTools.Method(typeof(Dialog_StylingStation), "DoWindowContents"), null, null, new HarmonyMethod(typeof(StylingStation), "DoWindowContentsTranspiler"));
		harmony.Patch(AccessTools.Constructor(typeof(Dialog_StylingStation), new Type[2]
		{
			typeof(Pawn),
			typeof(Thing)
		}), null, new HarmonyMethod(typeof(StylingStation), "ConstructorPostfix"));
		harmony.Patch(AccessTools.Method(typeof(Dialog_StylingStation), "Reset"), null, new HarmonyMethod(typeof(StylingStation), "ResetPostfix"));
		harmony.Patch(AccessTools.Method(typeof(ApparelProperties), "PawnCanWear", new Type[2]
		{
			typeof(Pawn),
			typeof(bool)
		}), null, new HarmonyMethod(patchType, "PawnCanWearPostfix"));
		harmony.Patch(AccessTools.Method(typeof(Scenario), "PostIdeoChosen"), new HarmonyMethod(patchType, "ScenarioPostIdeoChosenPrefix"));
		harmony.Patch(AccessTools.Method(typeof(StartingPawnUtility), "RegenerateStartingPawnInPlace"), new HarmonyMethod(patchType, "RegenerateStartingPawnInPlacePrefix"));
		harmony.Patch(AccessTools.PropertyGetter(typeof(Pawn_AgeTracker), "GrowthPointsFactor"), null, new HarmonyMethod(patchType, "GrowthPointsFactorPostfix"));
		harmony.Patch(AccessTools.Method(typeof(StatPart_Age), "AgeMultiplier"), new HarmonyMethod(patchType, "StatPartAgeMultiplierPrefix"));
		harmony.Patch(AccessTools.Method(typeof(GameComponent_PawnDuplicator), "Duplicate"), null, new HarmonyMethod(patchType, "DuplicatePostfix"));
		harmony.Patch(AccessTools.PropertySetter(typeof(Pawn_DraftController), "Drafted"), null, new HarmonyMethod(patchType, "DraftedPostfix"));
		harmony.Patch(AccessTools.Method(typeof(Pawn_MutantTracker), "Turn"), null, new HarmonyMethod(patchType, "MutantTurnPostfix"));
		harmony.Patch(AccessTools.Method(typeof(PawnGenerator), "FinalLevelOfSkill"), null, null, new HarmonyMethod(patchType, "FinalLevelOfSkillTranspiler"));
		harmony.Patch(AccessTools.PropertySetter(typeof(Need), "CurLevel"), null, new HarmonyMethod(patchType, "NeedLevelPostfix"));
		harmony.Patch(AccessTools.Method(typeof(Building_OutfitStand), "RecacheGraphics"), new HarmonyMethod(patchType, "OutfitStandEnableOverride"), new HarmonyMethod(patchType, "OutfitStandDisableOverride"), new HarmonyMethod(patchType, "OutfindStandRecacheGraphicsTranspiler"));
		harmony.Patch(AccessTools.Method(typeof(Building_OutfitStand), "DrawAt"), null, null, new HarmonyMethod(patchType, "OutfitStandDrawAtTranspiler"));
		harmony.Patch(AccessTools.PropertyGetter(typeof(Building_OutfitStand), "BodyTypeDefForRendering"), null, new HarmonyMethod(patchType, "OutfitStandBodyTypeDefForRenderingPostfix"));
		harmony.Patch(AccessTools.Method(typeof(Building_OutfitStand), "RimWorld.IHaulDestination.Accepts"), null, new HarmonyMethod(patchType, "OutfitStandAcceptsPostfix"));
		harmony.Patch(AccessTools.Method(typeof(CompStatue), "CreateSnapshotOfPawn_HookForMods"), null, new HarmonyMethod(patchType, "StatueSnapshotHookPostfix"));
		harmony.Patch(AccessTools.Method(typeof(CompStatue), "InitFakePawn_HookForMods"), null, new HarmonyMethod(patchType, "StatueFakePawnHookPostfix"));
		harmony.Patch(AccessTools.Method(typeof(CompStatue), "InitFakePawn"), null, null, new HarmonyMethod(patchType, "StatueInitFakePawnTranspiler"));
		harmony.Patch(AccessTools.Method(typeof(Pawn_GeneTracker), "AddictionChanceFactor"), new HarmonyMethod(patchType, "AddictionChanceFactorPrefix"));
		harmony.Patch(AccessTools.Method(typeof(JoyGiver_SocialRelax), "TryFindIngestibleToNurse"), null, null, new HarmonyMethod(patchType, "IngestibleToNurseTranspiler"));
		harmony.Patch(AccessTools.Method(typeof(PawnUtility), "CanTakeDrug"), null, new HarmonyMethod(patchType, "CanTakeDrugPostfix"));
		harmony.Patch(AccessTools.Method(typeof(MeditationFocusTypeAvailabilityCache), "PawnCanUseInt"), null, null, new HarmonyMethod(patchType, "PawnCanUseMeditationFocusTranspiler"));
		harmony.Patch(AccessTools.Method(typeof(Recipe_AdministerIngestible), "AvailableOnNow"), null, new HarmonyMethod(patchType, "IngestibleAvailableOnNowPostfix"));
		harmony.Patch(AccessTools.Method(typeof(Building_OutfitStand), "HeadOffsetAt"), null, new HarmonyMethod(patchType, "OutfitStandHeadOffsetAtPostfix"), new HarmonyMethod(patchType, "OutfitStandHeadOffsetAtTranspiler"));
		harmony.Patch((from memberInfo in AccessTools.GetDeclaredMethods(typeof(JobDriver_Lovin))
			where memberInfo.HasAttribute<CompilerGeneratedAttribute>()
			select memberInfo).OrderByDescending(delegate(MethodInfo methodInfo)
		{
			MethodBody methodBody = methodInfo.GetMethodBody();
			return (methodBody != null) ? methodBody.GetILAsByteArray().Length : 0;
		}).First(), null, null, new HarmonyMethod(patchType, "JobDriverLovinFinishTranspiler"));
		AlienRenderTreePatches.HarmonyInit(harmony);
		foreach (ThingDef_AlienRace ar in DefDatabase<ThingDef_AlienRace>.AllDefsListForReading)
		{
			ar.alienRace.raceRestriction?.workGiverList?.ForEach(delegate(WorkGiverDef wgd)
			{
				if (wgd != null)
				{
					harmony.Patch(AccessTools.Method(wgd.giverClass, "JobOnThing"), null, new HarmonyMethod(patchType, "GenericJobOnThingPostfix"));
					MethodInfo methodInfo = AccessTools.Method(wgd.giverClass, "HasJobOnThing");
					if ((object)methodInfo != null && methodInfo.IsDeclaredMember())
					{
						harmony.Patch(methodInfo, null, new HarmonyMethod(patchType, "GenericHasJobOnThingPostfix"));
					}
				}
			});
		}
		foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
		{
			AnimalBodyAddons extension = def.GetModExtension<AnimalBodyAddons>();
			if (extension != null)
			{
				extension.GenerateAddonData(def);
				def.comps.Add(new CompProperties(typeof(AnimalComp)));
			}
		}
		FieldInfo bodyInfo = AccessTools.Field(typeof(RaceProperties), "body");
		MethodInfo bodyCheck = AccessTools.Method(patchType, "ReplacedBody");
		HarmonyMethod bodyTranspiler = new HarmonyMethod(patchType, "BodyReferenceTranspiler");
		foreach (MethodInfo mi in (from t in typeof(LogEntry).Assembly.GetTypes().SelectMany((Type t) => t.GetNestedTypes(AccessTools.all).Concat(t))
			where (!t.IsAbstract || t.IsSealed) && !typeof(Delegate).IsAssignableFrom(t) && !t.IsGenericType
			select t).SelectMany((Type t) => from methodInfo in t.GetMethods(AccessTools.all).Concat(t.GetProperties(AccessTools.all).SelectMany((PropertyInfo pi) => pi.GetAccessors(nonPublic: true)))
			where methodInfo != null && !methodInfo.IsAbstract && methodInfo.DeclaringType == t && !methodInfo.IsGenericMethod && !methodInfo.HasAttribute<DllImportAttribute>()
			select methodInfo).Distinct())
		{
			IEnumerable<KeyValuePair<OpCode, object>> instructions = PatchProcessor.ReadMethodBody(mi);
			if (mi != bodyCheck && instructions.Any((KeyValuePair<OpCode, object> il) => il.Value?.Equals(bodyInfo) ?? false))
			{
				harmony.Patch(mi, null, null, bodyTranspiler);
			}
		}
		MethodInfo postureInfo = AccessTools.Method(typeof(PawnUtility), "GetPosture");
		foreach (MethodInfo mi2 in from methodInfo in typeof(PawnRenderer).GetMethods(AccessTools.all).Concat(typeof(PawnRenderer).GetProperties(AccessTools.all).SelectMany((PropertyInfo pi) => new List<MethodInfo>
			{
				pi.GetGetMethod(nonPublic: true),
				pi.GetGetMethod(nonPublic: false),
				pi.GetSetMethod(nonPublic: true),
				pi.GetSetMethod(nonPublic: false)
			}))
			where methodInfo != null && methodInfo.DeclaringType == typeof(PawnRenderer) && !methodInfo.IsGenericMethod
			select methodInfo)
		{
			IEnumerable<KeyValuePair<OpCode, object>> instructions2 = PatchProcessor.ReadMethodBody(mi2);
			if (instructions2.Any((KeyValuePair<OpCode, object> il) => il.Value?.Equals(postureInfo) ?? false))
			{
				harmony.Patch(mi2, null, null, new HarmonyMethod(patchType, "PostureTranspiler"));
			}
		}
		Log.Message("Alien race successfully completed " + harmony.PatchReport + " with harmony.");
		HairDefOf.Bald.styleTags.Add("alienNoStyle");
		BeardDefOf.NoBeard.styleTags.Add("alienNoStyle");
		TattooDefOf.NoTattoo_Body.styleTags.Add("alienNoStyle");
		TattooDefOf.NoTattoo_Face.styleTags.Add("alienNoStyle");
		AlienRaceMod.settings.UpdateSettings();
	}

	public static void IngestibleAvailableOnNowPostfix(Thing thing, ref bool __result, RecipeDef ___recipe)
	{
		if (__result && thing is Pawn pawn)
		{
			__result = RaceRestrictionSettings.CanEat(___recipe.ingredients[0].filter.AllowedThingDefs.First(), pawn.def);
		}
	}

	public static IEnumerable<CodeInstruction> PawnCanUseMeditationFocusTranspiler(IEnumerable<CodeInstruction> instructions)
	{
		List<CodeInstruction> instructionList = instructions.ToList();
		int index = instructionList.FindLastIndex((CodeInstruction ci) => ci.opcode == OpCodes.Ldc_I4_0);
		for (int i = 0; i < instructionList.Count; i++)
		{
			CodeInstruction instruction = instructionList[i];
			if (i == index)
			{
				yield return new CodeInstruction(OpCodes.Ldarg_0).WithLabels(instruction.labels);
				yield return new CodeInstruction(OpCodes.Ldarg_1);
				yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(patchType, "RaceCanUseMeditationFocusHelper"));
			}
			else
			{
				yield return instruction;
			}
		}
	}

	public static bool RaceCanUseMeditationFocusHelper(Pawn p, MeditationFocusDef focus)
	{
		if (p.def is ThingDef_AlienRace alienProps)
		{
			return alienProps.alienRace.generalSettings.meditationFocii.Contains(focus);
		}
		return false;
	}

	public static void CanTakeDrugPostfix(Pawn pawn, ThingDef drug, ref bool __result)
	{
		if (__result)
		{
			__result = RaceRestrictionSettings.CanEat(drug, pawn.def);
		}
	}

	public static IEnumerable<CodeInstruction> IngestibleToNurseTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
	{
		FieldInfo ingestibleInfo = AccessTools.Field(typeof(ThingDef), "ingestible");
		FieldInfo nurseableInfo = AccessTools.Field(typeof(IngestibleProperties), "nurseable");
		List<CodeInstruction> instructionList = instructions.ToList();
		Label skipLabel = ilg.DefineLabel();
		for (int i = 0; i < instructionList.Count; i++)
		{
			CodeInstruction instruction = instructionList[i];
			if (instruction.LoadsField(ingestibleInfo) && instructionList[i + 1].LoadsField(nurseableInfo))
			{
				yield return new CodeInstruction(OpCodes.Dup);
				yield return new CodeInstruction(OpCodes.Ldarg_1);
				yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Pawn), "def"));
				yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(RaceRestrictionSettings), "CanEat"));
				yield return new CodeInstruction(OpCodes.Brtrue, skipLabel);
				yield return new CodeInstruction(OpCodes.Pop);
				yield return new CodeInstruction(OpCodes.Br, instructionList[i + 2].operand);
				yield return new CodeInstruction(OpCodes.Nop).WithLabels(skipLabel);
			}
			yield return instruction;
		}
	}

	public static bool AddictionChanceFactorPrefix(ref float __result, Pawn ___pawn, ChemicalDef chemical)
	{
		if (!(___pawn.def is ThingDef_AlienRace alienProps))
		{
			return true;
		}
		if (!alienProps.alienRace.generalSettings.CanUseChemical(chemical))
		{
			__result = 0f;
			return false;
		}
		return true;
	}

	public static IEnumerable<CodeInstruction> StatueInitFakePawnTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
	{
		FieldInfo humanInfo = AccessTools.Field(typeof(ThingDefOf), "Human");
		FieldInfo colonistInfo = AccessTools.Field(typeof(PawnKindDefOf), "Colonist");
		MethodInfo wornApparelInfo = AccessTools.PropertyGetter(typeof(Pawn_ApparelTracker), "WornApparel");
		LocalBuilder modData = ilg.DeclareLocal(typeof(HARStatueContainer));
		LocalBuilder thingOwner = ilg.DeclareLocal(typeof(ThingOwner<Apparel>));
		List<CodeInstruction> instructionList = instructions.ToList();
		for (int i = 0; i < instructionList.Count; i++)
		{
			CodeInstruction instruction = instructionList[i];
			if (instruction.LoadsField(humanInfo))
			{
				yield return new CodeInstruction(OpCodes.Ldarg_0);
				yield return CodeInstruction.LoadField(typeof(CompStatue), "additionalSavedPawnDataForMods");
				yield return new CodeInstruction(OpCodes.Ldstr, HARStatueContainer.loadKey);
				yield return new CodeInstruction(OpCodes.Ldloca_S, modData.LocalIndex);
				yield return CodeInstruction.Call(AccessTools.Field(typeof(CompStatue), "additionalSavedPawnDataForMods").FieldType, "TryGetValue");
				yield return new CodeInstruction(OpCodes.Pop);
				yield return new CodeInstruction(OpCodes.Ldloc_S, modData.LocalIndex);
				Label trueLabel = ilg.DefineLabel();
				yield return new CodeInstruction(OpCodes.Brtrue, trueLabel);
				yield return instruction;
				Label falseLabel = ilg.DefineLabel();
				yield return new CodeInstruction(OpCodes.Br, falseLabel);
				yield return new CodeInstruction(OpCodes.Ldloc_S, modData.LocalIndex).WithLabels(trueLabel);
				yield return CodeInstruction.LoadField(typeof(HARStatueContainer), "alienRace");
				yield return new CodeInstruction(OpCodes.Castclass, typeof(ThingDef));
				yield return new CodeInstruction(OpCodes.Nop).WithLabels(falseLabel);
				continue;
			}
			if (instruction.Calls(wornApparelInfo) && instructionList[i + 2].Calls(AccessTools.Method(typeof(List<Apparel>), "Add")))
			{
				yield return new CodeInstruction(OpCodes.Dup);
				yield return CodeInstruction.LoadField(typeof(Pawn_ApparelTracker), "wornApparel");
				yield return CodeInstruction.StoreLocal(thingOwner.LocalIndex);
				yield return new CodeInstruction(instructionList[i + 1]);
				yield return CodeInstruction.LoadLocal(thingOwner.LocalIndex);
				yield return CodeInstruction.StoreField(typeof(Thing), "holdingOwner");
			}
			yield return instruction;
			if (instruction.LoadsField(colonistInfo))
			{
				yield return new CodeInstruction(OpCodes.Ldloc_S, modData.LocalIndex);
				Label falseLabel = ilg.DefineLabel();
				yield return new CodeInstruction(OpCodes.Brfalse, falseLabel);
				yield return new CodeInstruction(OpCodes.Pop);
				yield return new CodeInstruction(OpCodes.Ldloc_S, modData.LocalIndex);
				yield return CodeInstruction.LoadField(typeof(HARStatueContainer), "kindDef");
				yield return new CodeInstruction(OpCodes.Nop).WithLabels(falseLabel);
			}
		}
	}

	public static void StatueFakePawnHookPostfix(Pawn fakePawn, Dictionary<string, object> additionalSavedPawnDataForMods)
	{
		if (!additionalSavedPawnDataForMods.TryGetValue(HARStatueContainer.loadKey, out var rawData) || !(rawData is HARStatueContainer statueData))
		{
			return;
		}
		AlienPartGenerator.AlienComp.CopyAlienData(statueData.alienComp, fakePawn.GetComp<AlienPartGenerator.AlienComp>());
		foreach (AlienPartGenerator.ExposableValueTuple<TraitDef, int> trait in statueData.traits)
		{
			fakePawn.story.traits.GainTrait(new Trait(trait.first, trait.second, forced: true), suppressConflicts: true);
		}
	}

	public static void StatueSnapshotHookPostfix(Pawn p, Dictionary<string, object> dictToStoreDataIn)
	{
		if (p.def is ThingDef_AlienRace alienRace)
		{
			dictToStoreDataIn[HARStatueContainer.loadKey] = new HARStatueContainer
			{
				alienComp = p.GetComp<AlienPartGenerator.AlienComp>(),
				alienRace = alienRace,
				kindDef = p.kindDef,
				traits = p.story?.traits?.allTraits.Select((Trait t) => new AlienPartGenerator.ExposableValueTuple<TraitDef, int>(t.def, t.Degree)).ToList()
			};
		}
	}

	public static IEnumerable<CodeInstruction> OutfitStandHeadOffsetAtTranspiler(IEnumerable<CodeInstruction> instructions)
	{
		FieldInfo offsetInfo = AccessTools.Field(typeof(BodyTypeDef), "headOffset");
		foreach (CodeInstruction instruction in instructions)
		{
			yield return instruction;
			if (instruction.OperandIs(offsetInfo))
			{
				yield return new CodeInstruction(OpCodes.Ldarg_0);
				yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(patchType, "OutfitStandHeadOffsetAtHelper"));
			}
		}
	}

	public static Vector2 OutfitStandHeadOffsetAtHelper(Vector2 offset, Building_OutfitStand outfitStand)
	{
		Comp_OutfitStandHAR comp = outfitStand.GetComp<Comp_OutfitStandHAR>();
		if (comp == null)
		{
			return offset;
		}
		LifeStageAge lifeStage = comp.Race.race.lifeStageAges.FirstOrDefault((LifeStageAge lsa) => lsa.def.developmentalStage.Juvenile() == comp.IsJuvenileBodyType);
		Vector2 vector;
		if (!(lifeStage is LifeStageAgeAlien ageAlien))
		{
			vector = Vector2.zero;
		}
		else
		{
			Gender gender = comp.gender;
			Vector2 vector2 = ((gender != Gender.Female) ? ageAlien.headOffset : ageAlien.headFemaleOffset);
			vector = vector2;
		}
		Vector2 alienHeadOffset = vector;
		return offset + alienHeadOffset;
	}

	public static void OutfitStandHeadOffsetAtPostfix(ref Vector3 __result, Rot4 rotation, Building_OutfitStand __instance)
	{
		Comp_OutfitStandHAR comp = __instance.GetComp<Comp_OutfitStandHAR>();
		if (comp != null)
		{
			LifeStageAge lifeStage = comp.Race.race.lifeStageAges.FirstOrDefault((LifeStageAge lsa) => lsa.def.developmentalStage.Juvenile() == comp.IsJuvenileBodyType);
			LifeStageAgeAlien stageAgeAlien = lifeStage as LifeStageAgeAlien;
			Vector3 offsetSpecific = ((comp.gender != Gender.Female) ? stageAgeAlien?.headOffsetDirectional : stageAgeAlien?.headFemaleOffsetDirectional)?.GetOffset(rotation)?.GetOffset(portrait: false, comp.BodyType, comp.HeadType) ?? Vector3.zero;
			__result += offsetSpecific;
		}
	}

	public static void OutfitStandAcceptsPostfix(Building_OutfitStand __instance, Thing t, ref bool __result)
	{
		if (!__result)
		{
			return;
		}
		Comp_OutfitStandHAR comp = __instance.GetComp<Comp_OutfitStandHAR>();
		if (comp != null)
		{
			if (t.def.IsApparel && !RaceRestrictionSettings.CanWear(t.def, comp.Race))
			{
				__result = false;
			}
			if (t.def.IsWeapon && !RaceRestrictionSettings.CanWear(t.def, comp.Race))
			{
				__result = false;
			}
		}
	}

	public static void OutfitStandBodyTypeDefForRenderingPostfix(Building_OutfitStand __instance, ref BodyTypeDef __result)
	{
		__result = __instance.GetComp<Comp_OutfitStandHAR>()?.BodyType ?? __result;
	}

	public static IEnumerable<CodeInstruction> OutfitStandDrawAtTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
	{
		FieldInfo bodyInfo = AccessTools.Field(typeof(Building_OutfitStand), "bodyGraphic");
		FieldInfo bodyChildInfo = AccessTools.Field(typeof(Building_OutfitStand), "bodyGraphicChild");
		FieldInfo headInfo = AccessTools.Field(typeof(Building_OutfitStand), "headGraphic");
		LocalBuilder bodyTypeLoc = ilg.DeclareLocal(typeof(BodyTypeDef));
		List<CodeInstruction> instructionList = instructions.ToList();
		for (int i = 0; i < instructionList.Count; i++)
		{
			CodeInstruction instruction = instructionList[i];
			if (instruction.LoadsField(bodyInfo) || instruction.LoadsField(bodyChildInfo))
			{
				yield return new CodeInstruction(OpCodes.Ldc_I4_1).MoveLabelsFrom(instruction);
				yield return new CodeInstruction(OpCodes.Ldarg_0);
				yield return CodeInstruction.Call(patchType, "OutfitStandDrawAtHelper");
				continue;
			}
			if (instruction.LoadsField(headInfo))
			{
				yield return new CodeInstruction(OpCodes.Ldc_I4_0).MoveLabelsFrom(instruction);
				yield return new CodeInstruction(OpCodes.Ldarg_0);
				yield return CodeInstruction.Call(patchType, "OutfitStandDrawAtHelper");
				continue;
			}
			if (i < instructionList.Count - 2 && instructionList[i + 1].opcode == OpCodes.Ceq)
			{
				yield return new CodeInstruction(OpCodes.Stloc, bodyTypeLoc);
				yield return new CodeInstruction(OpCodes.Ldloc, bodyTypeLoc);
			}
			yield return instruction;
			if (instruction.opcode == OpCodes.Ceq)
			{
				yield return new CodeInstruction(OpCodes.Ldloc, bodyTypeLoc);
				yield return CodeInstruction.LoadField(typeof(BodyTypeDefOf), "Baby");
				yield return new CodeInstruction(OpCodes.Ceq);
				yield return new CodeInstruction(OpCodes.Or);
			}
		}
	}

	public static Graphic_Multi OutfitStandDrawAtHelper(bool body, Building_OutfitStand __instance)
	{
		object obj;
		if (body)
		{
			obj = (outfitStandComp = __instance.GetComp<Comp_OutfitStandHAR>())?.bodyGraphic ?? CachedData.outfitStandBodyGraphic();
		}
		else
		{
			obj = outfitStandComp?.headGraphic;
			if (obj == null)
			{
				return CachedData.outfitStandHeadGraphic();
			}
		}
		return (Graphic_Multi)obj;
	}

	public static IEnumerable<CodeInstruction> OutfindStandRecacheGraphicsTranspiler(IEnumerable<CodeInstruction> instructions)
	{
		foreach (CodeInstruction instruction in instructions)
		{
			yield return instruction;
			if (instruction.opcode == OpCodes.Ldloc_S && ((LocalBuilder)instruction.operand).LocalIndex == 9)
			{
				yield return CodeInstruction.Call(patchType, "OutfitStandRecacheGraphicsHelper");
			}
		}
	}

	public static Vector3 OutfitStandRecacheGraphicsHelper(Vector3 original)
	{
		Vector2 drawSize = (outfitStandComp.Race as ThingDef_AlienRace)?.alienRace.generalSettings.alienPartGenerator.customDrawSize ?? Vector2.one;
		original.x *= drawSize.x;
		original.z *= drawSize.y;
		return original;
	}

	public static void OutfitStandEnableOverride(Building_OutfitStand __instance)
	{
		outfitStandComp = __instance.GetComp<Comp_OutfitStandHAR>();
		AlienPartGenerator.ExtendedGraphicTop.drawOverrideDummy = ((outfitStandComp != null) ? new DummyExtendedGraphicsPawnWrapper
		{
			race = outfitStandComp.Race,
			bodyType = outfitStandComp.BodyType,
			headType = outfitStandComp.HeadType
		} : null);
	}

	public static void OutfitStandDisableOverride()
	{
		AlienPartGenerator.ExtendedGraphicTop.drawOverrideDummy = null;
	}

	public static void NeedLevelPostfix(Pawn ___pawn)
	{
		AlienPartGenerator.AlienComp.RegenerateAddonGraphicsWithCondition(___pawn, new HashSet<Type>
		{
			typeof(ConditionMood),
			typeof(ConditionNeed)
		});
	}

	public static IEnumerable<CodeInstruction> FinalLevelOfSkillTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
	{
		FieldInfo ageSkillMaxFactorCurveInfo = AccessTools.Field(typeof(PawnGenerator), "AgeSkillMaxFactorCurve");
		FieldInfo ageSkillCurve = AccessTools.Field(typeof(PawnGenerator), "AgeSkillFactor");
		LocalBuilder customCurve = ilg.DeclareLocal(typeof(bool));
		LocalBuilder originalValue = ilg.DeclareLocal(typeof(float));
		Label nonCustomLabel = ilg.DefineLabel();
		bool entryReady = false;
		foreach (CodeInstruction instruction in instructions)
		{
			if (instruction.LoadsField(ageSkillCurve))
			{
				yield return new CodeInstruction(OpCodes.Stloc, originalValue.LocalIndex);
				yield return new CodeInstruction(OpCodes.Ldloc_0);
				entryReady = true;
			}
			if (entryReady && instruction.opcode == OpCodes.Stloc_0)
			{
				entryReady = false;
				yield return new CodeInstruction(OpCodes.Ldloc, customCurve);
				yield return new CodeInstruction(OpCodes.Brfalse, nonCustomLabel);
				instruction.labels.Add(nonCustomLabel);
				yield return new CodeInstruction(OpCodes.Pop);
				yield return new CodeInstruction(OpCodes.Ldloc, originalValue.LocalIndex);
			}
			yield return instruction;
			if (instruction.LoadsField(ageSkillMaxFactorCurveInfo))
			{
				yield return new CodeInstruction(OpCodes.Ldarg_0);
				yield return new CodeInstruction(OpCodes.Ldloca, customCurve.LocalIndex);
				yield return CodeInstruction.Call(patchType, "SkillCurve");
			}
		}
	}

	public static SimpleCurve SkillCurve(SimpleCurve originalCurve, Pawn pawn, out bool customPresent)
	{
		SimpleCurve curve = (pawn.def as ThingDef_AlienRace)?.alienRace.generalSettings.ageSkillFactorCurve;
		customPresent = curve != null;
		return curve ?? originalCurve;
	}

	public static void MutantTurnPostfix(Pawn ___pawn)
	{
		AlienPartGenerator.AlienComp alienComp = ___pawn.GetComp<AlienPartGenerator.AlienComp>();
		if (alienComp != null)
		{
			alienComp.UpdateColors();
			alienComp.RegenerateAddonsForced();
		}
	}

	public static void DraftedPostfix(Pawn ___pawn)
	{
		AlienPartGenerator.AlienComp.RegenerateAddonGraphicsWithCondition(___pawn, new HashSet<Type> { typeof(ConditionDrafted) });
	}

	public static void DuplicatePostfix(Pawn pawn, Pawn __result)
	{
		AlienPartGenerator.AlienComp.CopyAlienData(pawn, __result);
	}

	public static bool StatPartAgeMultiplierPrefix(ref float __result, StatPart_Age __instance, Pawn pawn)
	{
		if (pawn.def is ThingDef_AlienRace race && race.alienRace.generalSettings.ageStatOverride.TryGetValue(__instance.parentStat, out var overridePart))
		{
			ref bool useBiologicalYears = ref CachedData.statPartAgeUseBiologicalYearsField(overridePart);
			ref SimpleCurve curve = ref CachedData.statPartAgeCurveField(overridePart);
			__result = (useBiologicalYears ? curve.Evaluate(pawn.ageTracker.AgeBiologicalYears) : curve.Evaluate(pawn.ageTracker.AgeBiologicalYearsFloat / pawn.RaceProps.lifeExpectancy));
			return false;
		}
		return true;
	}

	public static void GrowthPointsFactorPostfix(Pawn_AgeTracker __instance, ref float __result, Pawn ___pawn)
	{
		if (___pawn.def is ThingDef_AlienRace alienProps && alienProps.alienRace.generalSettings.growthFactorByAge != null)
		{
			__result = alienProps.alienRace.generalSettings.growthFactorByAge.Evaluate(__instance.AgeBiologicalYears);
		}
	}

	public static void RegenerateStartingPawnInPlacePrefix()
	{
		firstStartingRequest = false;
	}

	public static void ScenarioPostIdeoChosenPrefix()
	{
		firstStartingRequest = true;
	}

	public static void PawnCanWearPostfix(ApparelProperties __instance, Pawn pawn, ref bool __result)
	{
		__result &= RaceRestrictionSettings.CanWear(CachedData.GetApparelFromApparelProps(__instance), pawn.def);
	}

	public static IEnumerable<CodeInstruction> HasGreyHairTranspiler(IEnumerable<CodeInstruction> instructions)
	{
		List<CodeInstruction> instructionList = instructions.ToList();
		for (int i = 0; i < instructionList.Count; i++)
		{
			CodeInstruction instruction = instructionList[i];
			if (instruction.opcode == OpCodes.Ldc_I4_S)
			{
				instruction.operand = -1;
			}
			if (instruction.opcode == OpCodes.Stloc_0)
			{
				yield return new CodeInstruction(OpCodes.Ldarg_0);
				yield return CodeInstruction.Call(patchType, "HasOldHairHelper");
			}
			yield return instruction;
		}
	}

	public static float HasOldHairHelper(float originalChance, Pawn pawn)
	{
		if (!(pawn.def is ThingDef_AlienRace alienProps))
		{
			return originalChance;
		}
		return alienProps.alienRace.generalSettings.alienPartGenerator.oldHairAgeCurve.Evaluate(pawn.ageTracker.AgeBiologicalYearsFloat);
	}

	public static IEnumerable<CodeInstruction> NewGeneratedStartingPawnTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg, MethodBase originalMethod)
	{
		MethodInfo getRequestInfo = AccessTools.Method(typeof(StartingPawnUtility), "GetGenerationRequest");
		List<CodeInstruction> instructionList = instructions.ToList();
		for (int i = 0; i < instructionList.Count; i++)
		{
			CodeInstruction instruction = instructionList[i];
			yield return instruction;
			if (instruction.Calls(getRequestInfo))
			{
				yield return CodeInstruction.LoadField(typeof(AlienRaceMod), "settings");
				yield return CodeInstruction.LoadField(typeof(AlienRaceSettings), "randomizeStartingPawnsOnReroll");
				yield return new CodeInstruction(OpCodes.Brfalse, instructionList[i + 1].operand);
				yield return CodeInstruction.LoadField(typeof(HarmonyPatches), "firstStartingRequest");
				yield return new CodeInstruction(OpCodes.Brtrue, instructionList[i + 1].operand);
				yield return new CodeInstruction(OpCodes.Stsfld, AccessTools.Field(patchType, "currentStartingRequest"));
				yield return new CodeInstruction(OpCodes.Ldarg_0);
				yield return new CodeInstruction(OpCodes.Call, instructionList[i + 2].operand);
				yield return new CodeInstruction(OpCodes.Dup);
				yield return new CodeInstruction(OpCodes.Stloc_0);
				yield return CodeInstruction.Call(typeof(StartingPawnUtility), "SetGenerationRequest");
				yield return new CodeInstruction(OpCodes.Ldsflda, AccessTools.Field(patchType, "currentStartingRequest"));
				yield return new CodeInstruction(OpCodes.Initobj, typeof(PawnGenerationRequest));
				yield return new CodeInstruction(OpCodes.Ldloc_0);
			}
		}
	}

	public static IEnumerable<CodeInstruction> LifestageAndXenotypeOptionsTranspiler(IEnumerable<CodeInstruction> instructions)
	{
		MethodInfo dbXenoInfo = AccessTools.PropertyGetter(typeof(DefDatabase<XenotypeDef>), "AllDefs");
		foreach (CodeInstruction instruction in instructions)
		{
			yield return instruction;
			if (instruction.Calls(dbXenoInfo))
			{
				yield return new CodeInstruction(OpCodes.Ldarg_0);
				yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Pawn), "def"));
				yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(patchType, "FilterXenotypeHelper"));
			}
		}
	}

	public static bool SetXenotypePrefix(XenotypeDef xenotype, Pawn ___pawn)
	{
		return RaceRestrictionSettings.CanUseXenotype(xenotype, ___pawn.def);
	}

	public static IEnumerable<CodeInstruction> GetXenotypeForGeneratedPawnTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
	{
		MethodInfo pawnGenAllowedXenotypeInfo = AccessTools.PropertyGetter(typeof(PawnGenerationRequest), "AllowedXenotypes");
		List<CodeInstruction> instructionList = instructions.ToList();
		for (int i = 0; i < instructionList.Count; i++)
		{
			CodeInstruction instruction = instructionList[i];
			yield return instruction;
			if (instruction.Calls(pawnGenAllowedXenotypeInfo) && instructionList[i + 1].opcode == OpCodes.Ldloca_S)
			{
				yield return new CodeInstruction(instructionList[i - 1]);
				yield return new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(PawnGenerationRequest), "KindDef"));
				yield return CodeInstruction.LoadField(typeof(PawnKindDef), "race");
				yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(patchType, "FilterXenotypeHelper"));
			}
		}
	}

	public static List<XenotypeDef> FilterXenotypeHelper(List<XenotypeDef> xenotypes, ThingDef race)
	{
		HashSet<XenotypeDef> removedXenotypes;
		return RaceRestrictionSettings.FilterXenotypes(xenotypes, race, out removedXenotypes).ToList();
	}

	public static void XenotypesAvailableForPostfix(PawnKindDef kind, ref Dictionary<XenotypeDef, float> __result)
	{
		RaceRestrictionSettings.FilterXenotypes(__result.Keys, kind.race, out var forbidden);
		foreach (XenotypeDef xenotypeDef in forbidden)
		{
			__result.Remove(xenotypeDef);
		}
	}

	public static IEnumerable<CodeInstruction> ApplyBirthOutcomeTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg, MethodBase originalMethod)
	{
		FieldInfo pawnKindInfo = AccessTools.Field(typeof(Pawn), "kindDef");
		FieldInfo countInfo = AccessTools.Field(patchType, "currentBirthCount");
		List<CodeInstruction> instructionList = instructions.ToList();
		for (int i = 0; i < instructionList.Count; i++)
		{
			CodeInstruction instruction = instructionList[i];
			yield return instruction;
			if (instruction.opcode == OpCodes.Ldarg_S && instructionList[i + 1].LoadsField(pawnKindInfo))
			{
				yield return new CodeInstruction(OpCodes.Ldarg, 6);
				yield return CodeInstruction.Call(patchType, "BirthOutcomeHelper");
				i++;
			}
			if (instruction.opcode == OpCodes.Stloc_0)
			{
				Label loop = ilg.DefineLabel();
				Label loopEnd = ilg.DefineLabel();
				Label loopSkip = ilg.DefineLabel();
				yield return new CodeInstruction(OpCodes.Ldsfld, countInfo);
				yield return new CodeInstruction(OpCodes.Ldc_I4, int.MinValue);
				yield return new CodeInstruction(OpCodes.Bne_Un, loopSkip);
				yield return new CodeInstruction(OpCodes.Ldarg, 4);
				yield return CodeInstruction.Call(patchType, "BirthOutcomeMultiplier");
				yield return new CodeInstruction(OpCodes.Stsfld, countInfo);
				yield return new CodeInstruction(OpCodes.Ldsfld, countInfo)
				{
					labels = new List<Label>(1) { loop }
				};
				yield return new CodeInstruction(OpCodes.Ldc_I4_1);
				yield return new CodeInstruction(OpCodes.Sub);
				yield return new CodeInstruction(OpCodes.Dup);
				yield return new CodeInstruction(OpCodes.Stsfld, countInfo);
				yield return new CodeInstruction(OpCodes.Ldc_I4_0);
				yield return new CodeInstruction(OpCodes.Blt_S, loopEnd);
				for (int j = 0; j < originalMethod.GetParameters().Length; j++)
				{
					yield return new CodeInstruction(OpCodes.Ldarg, j);
				}
				yield return new CodeInstruction(OpCodes.Call, originalMethod);
				yield return new CodeInstruction(OpCodes.Br, loop);
				yield return new CodeInstruction(OpCodes.Ldc_I4, int.MinValue)
				{
					labels = new List<Label> { loopEnd }
				};
				yield return new CodeInstruction(OpCodes.Stsfld, countInfo);
				yield return new CodeInstruction(OpCodes.Nop)
				{
					labels = new List<Label>(1) { loopSkip }
				};
			}
		}
	}

	public static int BirthOutcomeMultiplier(Pawn mother)
	{
		if (mother == null)
		{
			return 0;
		}
		return Mathf.RoundToInt(Rand.ByCurve(mother.RaceProps.litterSizeCurve)) - 1;
	}

	public static PawnKindDef BirthOutcomeHelper(Pawn mother, Pawn partner)
	{
		if (!(mother?.def is ThingDef_AlienRace alienProps))
		{
			return mother?.kindDef;
		}
		PawnKindDef kindDef = alienProps.alienRace.generalSettings.reproduction.childKindDef;
		if (partner != null)
		{
			List<HybridSpecificSettings> hybrids = alienProps.alienRace.generalSettings.reproduction.hybridSpecific.Where((HybridSpecificSettings hss) => hss.partnerRace == partner.def).ToList();
			if (hybrids.Any() && hybrids.TryRandomElementByWeight((HybridSpecificSettings hss) => hss.probability, out var res))
			{
				kindDef = res.childKindDef;
			}
		}
		return kindDef ?? mother.kindDef;
	}

	public static void AdultLifeStageStartedPostfix(Pawn pawn)
	{
		LongEventHandler.ExecuteWhenFinished(delegate
		{
			List<BackstoryTrait> list = pawn.story?.Adulthood?.forcedTraits;
			if (!list.NullOrEmpty())
			{
				foreach (BackstoryTrait current in list)
				{
					if (current.def == null)
					{
						Log.Error("Null forced trait def on " + pawn.story.Adulthood);
					}
					else if (!pawn.story.traits.HasTrait(current.def))
					{
						pawn.story.traits.GainTrait(new Trait(current.def, current.degree));
					}
				}
			}
		});
	}

	public static void ChildLifeStageStartedPostfix(Pawn pawn)
	{
		LongEventHandler.ExecuteWhenFinished(delegate
		{
			List<BackstoryTrait> list = pawn.story?.Childhood?.forcedTraits;
			if (!list.NullOrEmpty())
			{
				foreach (BackstoryTrait current in list)
				{
					if (current.def == null)
					{
						Log.Error("Null forced trait def on " + pawn.story.Childhood);
					}
					else if (!pawn.story.traits.HasTrait(current.def))
					{
						pawn.story.traits.GainTrait(new Trait(current.def, current.degree));
					}
				}
			}
		});
	}

	public static IEnumerable<CodeInstruction> WandererJoinTranspiler(IEnumerable<CodeInstruction> instructions)
	{
		foreach (CodeInstruction instruction in instructions)
		{
			yield return instruction;
			if (instruction.opcode == OpCodes.Ldloc_1)
			{
				yield return CodeInstruction.Call(patchType, "WandererJoinHelper");
			}
		}
	}

	public static PawnGenerationRequest WandererJoinHelper(PawnGenerationRequest request)
	{
		PawnKindDef kindDef = request.KindDef;
		if (kindDef.race != Faction.OfPlayerSilentFail?.def.basicMemberKind.race)
		{
			kindDef = Faction.OfPlayerSilentFail?.def.basicMemberKind ?? kindDef;
		}
		if ((from fpke in DefDatabase<RaceSettings>.AllDefsListForReading.Where((RaceSettings tdar) => !tdar.pawnKindSettings.alienwandererkinds.NullOrEmpty()).SelectMany((RaceSettings rs) => rs.pawnKindSettings.alienwandererkinds)
			where fpke.factionDefs.Contains(Faction.OfPlayer.def)
			select fpke).SelectMany((FactionPawnKindEntry fpke) => fpke.pawnKindEntries).TryRandomElementByWeight((PawnKindEntry pke) => pke.chance, out var pk))
		{
			kindDef = pk.kindDefs.RandomElement();
		}
		request.KindDef = kindDef;
		return request;
	}

	public static void EmbryoImplantReportPostfix(HumanEmbryo __instance, Pawn pawn, ref AcceptanceReport __result)
	{
		Pawn second = __instance.TryGetComp<CompHasPawnSources>().pawnSources?.FirstOrDefault();
		if (second != null && pawn != null && second.def != pawn.def)
		{
			__result = false;
		}
	}

	public static void EmbryoImplantPawnPrefix(HumanEmbryo __instance, ref bool cancel)
	{
		if (__instance.implantTarget is Pawn pawn)
		{
			Pawn second = __instance.TryGetComp<CompHasPawnSources>().pawnSources?.FirstOrDefault();
			cancel = second != null && second.def != pawn.def;
		}
	}

	public static IEnumerable<CodeInstruction> AdultLifeStageStartedTranspiler(IEnumerable<CodeInstruction> instructions)
	{
		FieldInfo backstoryFilters = AccessTools.Field(typeof(LifeStageWorker_HumanlikeAdult), "VatgrowBackstoryFilter");
		FieldInfo backstoryTribalFilters = AccessTools.Field(typeof(LifeStageWorker_HumanlikeAdult), "BackstoryFiltersTribal");
		MethodInfo isPlayerColonyChildBackstory = AccessTools.PropertyGetter(typeof(BackstoryDef), "IsPlayerColonyChildBackstory");
		List<CodeInstruction> instructionList = instructions.ToList();
		for (int index = 0; index < instructionList.Count; index++)
		{
			CodeInstruction instruction = instructionList[index];
			if (instruction.Calls(isPlayerColonyChildBackstory))
			{
				yield return new CodeInstruction(OpCodes.Ldarg_1)
				{
					labels = instruction.ExtractLabels()
				};
				yield return CodeInstruction.Call(patchType, "IsPlayerColonyChildBackstoryHelper");
			}
			else
			{
				yield return instruction;
			}
			if (instruction.LoadsField(backstoryTribalFilters))
			{
				yield return new CodeInstruction(OpCodes.Ldarg_1).MoveLabelsFrom(instructionList[index + 1]);
				yield return new CodeInstruction(OpCodes.Ldc_I4_1);
				yield return CodeInstruction.Call(patchType, "LifeStageStartedHelper");
			}
			if (instruction.LoadsField(backstoryFilters))
			{
				yield return new CodeInstruction(OpCodes.Ldarg_1);
				yield return new CodeInstruction(OpCodes.Ldc_I4_2);
				yield return CodeInstruction.Call(patchType, "LifeStageStartedHelper");
			}
		}
	}

	public static IEnumerable<CodeInstruction> ChildLifeStageStartedTranspiler(IEnumerable<CodeInstruction> instructions)
	{
		FieldInfo backstoryFilters = AccessTools.Field(typeof(LifeStageWorker_HumanlikeChild), "ChildBackstoryFilters");
		foreach (CodeInstruction instruction in instructions)
		{
			yield return instruction;
			if (instruction.LoadsField(backstoryFilters))
			{
				yield return new CodeInstruction(OpCodes.Ldarg_1);
				yield return new CodeInstruction(OpCodes.Ldc_I4_0);
				yield return CodeInstruction.Call(patchType, "LifeStageStartedHelper");
			}
		}
	}

	public static List<BackstoryCategoryFilter> LifeStageStartedHelper(List<BackstoryCategoryFilter> filters, Pawn pawn, int backstoryKind)
	{
		if (pawn.def is ThingDef_AlienRace alienProps)
		{
			List<BackstoryCategoryFilter> filtersNew = backstoryKind switch
			{
				0 => alienProps.alienRace.generalSettings.childBackstoryFilter, 
				1 => alienProps.alienRace.generalSettings.adultBackstoryFilter, 
				2 => alienProps.alienRace.generalSettings.adultVatBackstoryFilter, 
				3 => alienProps.alienRace.generalSettings.newbornBackstoryFilter, 
				_ => null, 
			};
			if (!filtersNew.NullOrEmpty())
			{
				return filtersNew;
			}
			return filters;
		}
		return filters;
	}

	public static bool IsPlayerColonyChildBackstoryHelper(BackstoryDef backstory, Pawn pawn)
	{
		if ((pawn.def as ThingDef_AlienRace)?.alienRace.generalSettings.childBackstoryFilter?.Any((BackstoryCategoryFilter bcf) => bcf.Matches(backstory)) != true)
		{
			return backstory.IsPlayerColonyChildBackstory;
		}
		return true;
	}

	public static void GetBackstoryCategoryFiltersForPostfix(Pawn pawn, ref List<BackstoryCategoryFilter> __result)
	{
		if (pawn.def is ThingDef_AlienRace && pawn.DevelopmentalStage.Juvenile())
		{
			int index = 0;
			if (pawn.DevelopmentalStage.Baby())
			{
				index = 3;
			}
			__result = LifeStageStartedHelper(__result, pawn, index);
		}
	}

	public static void HumanOvumCanFertilizeReportPostfix(Pawn pawn, ref AcceptanceReport __result)
	{
		if (!__result.Accepted)
		{
			return;
		}
		Pawn second = pawn.TryGetComp<CompHasPawnSources>()?.pawnSources?.FirstOrDefault();
		bool num;
		if (second == null)
		{
			ThingDef_AlienRace obj = pawn.def as ThingDef_AlienRace;
			if (obj == null)
			{
				goto IL_005c;
			}
			num = !obj.alienRace.raceRestriction.canReproduce;
		}
		else
		{
			num = !RaceRestrictionSettings.CanReproduce(second, pawn);
		}
		if (!num)
		{
			return;
		}
		goto IL_005c;
		IL_005c:
		__result = false;
	}

	public static void ExtractOvumAvailableReportPostfix(Thing thing, ref AcceptanceReport __result)
	{
		if (__result.Accepted && thing.def is ThingDef_AlienRace alienProps && !alienProps.alienRace.raceRestriction.canReproduce)
		{
			__result = false;
		}
	}

	public static IEnumerable<CodeInstruction> JobDriverLovinFinishTranspiler(IEnumerable<CodeInstruction> instructions)
	{
		List<CodeInstruction> instructionList = instructions.ToList();
		FieldInfo genderInfo = AccessTools.Field(typeof(Pawn), "gender");
		FieldInfo pawnInfo = AccessTools.Field(typeof(JobDriver), "pawn");
		MethodInfo partnerInfo = AccessTools.PropertyGetter(typeof(JobDriver_Lovin), "Partner");
		int state = 0;
		for (int i = 0; i < instructionList.Count; i++)
		{
			CodeInstruction instruction = instructionList[i];
			if (state < 4 && instructionList[i + 1].LoadsField(genderInfo))
			{
				if ((uint)state <= 1u)
				{
					yield return instruction;
				}
				else
				{
					yield return instruction.LoadsField(pawnInfo) ? new CodeInstruction(OpCodes.Call, partnerInfo) : new CodeInstruction(OpCodes.Ldfld, pawnInfo);
				}
				bool flag = (uint)state <= 1u;
				yield return new CodeInstruction(flag ? OpCodes.Ldc_I4_0 : OpCodes.Ldc_I4_1);
				yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ReproductionSettings), "ApplicableGender", new Type[2]
				{
					typeof(Pawn),
					typeof(bool)
				}));
				if ((uint)(state - 2) <= 1u)
				{
					i++;
					yield return new CodeInstruction(OpCodes.Ldc_I4_1);
				}
				state++;
				i++;
			}
			else if ((uint)(state - 4) <= 1u)
			{
				if (instruction.Calls(partnerInfo))
				{
					yield return new CodeInstruction(OpCodes.Ldfld, pawnInfo);
					state++;
				}
				else if (instruction.LoadsField(pawnInfo))
				{
					yield return new CodeInstruction(OpCodes.Call, partnerInfo);
					state++;
				}
				else
				{
					yield return instruction;
				}
			}
			else
			{
				yield return instruction;
			}
		}
	}

	public static IEnumerable<CodeInstruction> CanEverProduceChildTranspiler(IEnumerable<CodeInstruction> instructions)
	{
		List<CodeInstruction> instructionList = instructions.ToList();
		FieldInfo genderInfo = AccessTools.Field(typeof(Pawn), "gender");
		int step = 0;
		for (int i = 0; i < instructionList.Count; i++)
		{
			CodeInstruction instruction = instructionList[i];
			if (instruction.LoadsField(genderInfo))
			{
				switch (step)
				{
				case 0:
					step++;
					yield return instructionList[i + 1];
					yield return CodeInstruction.Call(typeof(ReproductionSettings), "GenderReproductionCheck");
					yield return new CodeInstruction(OpCodes.Brtrue, instructionList[i + 3].operand);
					i += 3;
					break;
				case 1:
					step++;
					yield return new CodeInstruction(OpCodes.Ldc_I4_0);
					yield return CodeInstruction.Call(typeof(ReproductionSettings), "ApplicableGender", new Type[2]
					{
						typeof(Pawn),
						typeof(bool)
					});
					yield return new CodeInstruction(OpCodes.Brtrue, instructionList[i + 2].operand);
					i += 2;
					break;
				case 2:
					step++;
					yield return new CodeInstruction(OpCodes.Ldc_I4_1);
					yield return CodeInstruction.Call(typeof(ReproductionSettings), "ApplicableGender", new Type[2]
					{
						typeof(Pawn),
						typeof(bool)
					});
					yield return new CodeInstruction(OpCodes.Brtrue, instructionList[i + 2].operand);
					i += 2;
					break;
				}
			}
			else
			{
				yield return instruction;
			}
		}
	}

	public static void CanEverProduceChildPostfix(Pawn first, Pawn second, ref AcceptanceReport __result)
	{
		if (__result.Accepted && !RaceRestrictionSettings.CanReproduce(first, second))
		{
			__result = "HAR.ReproductionNotAllowed".Translate(new NamedArgument(first.gender.GetLabel(), "genderOne"), new NamedArgument(first.def.LabelCap, "raceOne"), new NamedArgument(second.gender.GetLabel(), "genderTwo"), new NamedArgument(second.def.LabelCap, "raceTwo"));
		}
	}

	public static bool PregnancyChanceForPartnersPrefix(Pawn woman, Pawn man, ref float __result)
	{
		if (!RaceRestrictionSettings.CanReproduce(woman, man))
		{
			__result = 0f;
			return false;
		}
		return true;
	}

	public static bool AddGenePrefix(Gene gene, Pawn ___pawn, ref Gene __result, bool addAsXenogene)
	{
		if (!RaceRestrictionSettings.CanHaveGene(gene.def, ___pawn.def, addAsXenogene))
		{
			__result = null;
			return false;
		}
		return true;
	}

	public static IEnumerable<CodeInstruction> FertilityAgeFactorTranspiler(IEnumerable<CodeInstruction> instructions)
	{
		FieldInfo maleInfo = AccessTools.Field(typeof(StatPart_FertilityByGenderAge), "maleFertilityAgeFactor");
		FieldInfo femaleInfo = AccessTools.Field(typeof(StatPart_FertilityByGenderAge), "femaleFertilityAgeFactor");
		foreach (CodeInstruction instruction in instructions)
		{
			yield return instruction;
			if (instruction.LoadsField(maleInfo))
			{
				yield return new CodeInstruction(OpCodes.Ldarg_1);
				yield return new CodeInstruction(OpCodes.Ldc_I4_1);
				yield return CodeInstruction.Call(patchType, "FertilityCurveHelper");
			}
			else if (instruction.LoadsField(femaleInfo))
			{
				yield return new CodeInstruction(OpCodes.Ldarg_1);
				yield return new CodeInstruction(OpCodes.Ldc_I4_2);
				yield return CodeInstruction.Call(patchType, "FertilityCurveHelper");
			}
		}
	}

	public static SimpleCurve FertilityCurveHelper(SimpleCurve original, Pawn pawn, Gender gender)
	{
		if (!(pawn.def is ThingDef_AlienRace alienProps))
		{
			return original;
		}
		if (gender != Gender.Female)
		{
			return alienProps.alienRace.generalSettings.reproduction.maleFertilityAgeFactor;
		}
		return alienProps.alienRace.generalSettings.reproduction.femaleFertilityAgeFactor;
	}

	public static void FinalizeLookChangePostfix(ref Toil __result)
	{
		Action initAction = __result.initAction;
		Toil toil = __result;
		__result.initAction = delegate
		{
			initAction();
			toil.actor.GetComp<AlienPartGenerator.AlienComp>()?.OverwriteColorChannel("hair", toil.actor.style.nextHairColor);
		};
	}

	public static void FinalizeHairColorPostfix(Pawn_StyleTracker __instance)
	{
		__instance.pawn.GetComp<AlienPartGenerator.AlienComp>()?.OverwriteColorChannel("hair", __instance.pawn.style.nextHairColor);
	}

	public static IEnumerable<CodeInstruction> GrowthMomentCacheLetterTextTranspiler(IEnumerable<CodeInstruction> instructions)
	{
		List<CodeInstruction> instructionList = instructions.ToList();
		FieldInfo growthMomentAgesInfo = AccessTools.Field(typeof(GrowthUtility), "GrowthMomentAges");
		foreach (CodeInstruction instruction in instructionList)
		{
			if (instruction.LoadsField(growthMomentAgesInfo))
			{
				yield return new CodeInstruction(OpCodes.Ldarg_0).MoveLabelsFrom(instruction);
				yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Pawn_AgeTracker), "pawn"));
				yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Pawn), "def"));
				yield return CodeInstruction.Call(patchType, "GrowthMomentHelper", new Type[1] { typeof(ThingDef) });
			}
			else
			{
				yield return instruction;
			}
		}
	}

	public static IEnumerable<CodeInstruction> TrySimulateGrowthPointsTranspiler(IEnumerable<CodeInstruction> instructions)
	{
		List<CodeInstruction> instructionList = instructions.ToList();
		FieldInfo growthMomentAgesInfo = AccessTools.Field(typeof(GrowthUtility), "GrowthMomentAges");
		FieldInfo growthMomentAgesCacheInfo = AccessTools.Field(typeof(Pawn_AgeTracker), "growthMomentAges");
		for (int i = 0; i < instructionList.Count; i++)
		{
			CodeInstruction instruction = instructionList[i];
			if (instruction.LoadsField(growthMomentAgesCacheInfo) && instructionList[i + 1].opcode == OpCodes.Ldnull)
			{
				yield return new CodeInstruction(OpCodes.Ldnull).MoveLabelsFrom(instruction);
				continue;
			}
			if (instruction.LoadsField(growthMomentAgesInfo))
			{
				yield return new CodeInstruction(OpCodes.Ldarg_0).MoveLabelsFrom(instruction);
				yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Pawn_AgeTracker), "pawn"));
				yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Pawn), "def"));
				yield return CodeInstruction.Call(patchType, "GrowthMomentHelper", new Type[1] { typeof(ThingDef) });
				continue;
			}
			if (instruction.opcode == OpCodes.Ldc_I4_3)
			{
				yield return new CodeInstruction(OpCodes.Ldarg_0).MoveLabelsFrom(instruction);
				yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Pawn_AgeTracker), "pawn"));
				yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Pawn), "def"));
				yield return CodeInstruction.Call(patchType, "GetBabyToChildAge");
				continue;
			}
			if (instruction.Is(OpCodes.Ldc_I4_S, 13))
			{
				yield return new CodeInstruction(OpCodes.Ldarg_0).MoveLabelsFrom(instruction);
				yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Pawn_AgeTracker), "pawn"));
				yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Pawn), "def"));
				yield return CodeInstruction.Call(patchType, "GetChildToAdultAge");
				continue;
			}
			if (instruction.Is(OpCodes.Ldc_R4, 7))
			{
				yield return new CodeInstruction(OpCodes.Dup);
			}
			else if (instruction.opcode == OpCodes.Mul && instructionList[i + 1].opcode == OpCodes.Stloc_2)
			{
				yield return new CodeInstruction(OpCodes.Ldarg_0).MoveLabelsFrom(instruction);
				yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Pawn_AgeTracker), "pawn"));
				yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Pawn), "def"));
				yield return CodeInstruction.Call(patchType, "EvalGrowthPointsHelper");
			}
			yield return instruction;
		}
	}

	public static float EvalGrowthPointsHelper(float age, float original, ThingDef pawnDef)
	{
		if (pawnDef is ThingDef_AlienRace alienProps && alienProps.alienRace.generalSettings.growthFactorByAge != null)
		{
			return alienProps.alienRace.generalSettings.growthFactorByAge.Evaluate(age);
		}
		return original;
	}

	public static int GetBabyToChildAge(ThingDef pawnDef)
	{
		return Mathf.FloorToInt(pawnDef.race.lifeStageAges.First((LifeStageAge lsa) => lsa.def.developmentalStage.HasAny(DevelopmentalStage.Child | DevelopmentalStage.Adult)).minAge);
	}

	public static int GetChildToAdultAge(ThingDef pawnDef)
	{
		return Mathf.FloorToInt(pawnDef.race.lifeStageAges.First((LifeStageAge lsa) => lsa.def.developmentalStage.HasAny(DevelopmentalStage.Adult)).minAge);
	}

	public static void GrowthTierTooltipPrefix(Pawn ___child)
	{
		growthMomentPawn = ___child;
	}

	public static void TryChildGrowthMomentPrefix(Pawn ___pawn)
	{
		growthMomentPawn = ___pawn;
	}

	public static void GenerateSkillsPrefix(Pawn pawn)
	{
		growthMomentPawn = pawn;
	}

	public static void GenerateTraitsPrefix(Pawn pawn)
	{
		growthMomentPawn = pawn;
	}

	public static IEnumerable<CodeInstruction> IsGrowthBirthdayTranspiler(IEnumerable<CodeInstruction> instructions)
	{
		foreach (CodeInstruction instruction in instructions)
		{
			if (instruction.opcode == OpCodes.Ldsfld)
			{
				yield return CodeInstruction.Call(patchType, "GrowthMomentHelper", Type.EmptyTypes);
			}
			else
			{
				yield return instruction;
			}
		}
	}

	public static int[] GrowthMomentHelper()
	{
		return GrowthMomentHelper(growthMomentPawn.def);
	}

	public static int[] GrowthMomentHelper(ThingDef pawnDef)
	{
		return (pawnDef as ThingDef_AlienRace)?.alienRace.generalSettings.GrowthAges ?? GrowthUtility.GrowthMomentAges;
	}

	public static IEnumerable<CodeInstruction> NotifyGenesChangedTranspiler(IEnumerable<CodeInstruction> instructions)
	{
		MethodInfo allDefsInfo = AccessTools.PropertyGetter(typeof(DefDatabase<HeadTypeDef>), "AllDefs");
		foreach (CodeInstruction instruction in instructions)
		{
			if (instruction.opcode == OpCodes.Stloc_1)
			{
				yield return new CodeInstruction(OpCodes.Pop);
				yield return new CodeInstruction(OpCodes.Ldc_I4_1);
			}
			yield return instruction;
			if (instruction.Calls(allDefsInfo))
			{
				yield return new CodeInstruction(OpCodes.Ldarg_0);
				yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Pawn_GeneTracker), "pawn"));
				yield return CodeInstruction.Call(patchType, "HeadTypeFilter");
			}
		}
	}

	public static void TryGenerateNewPawnInternalPrefix(ref PawnGenerationRequest request)
	{
		if (!request.KindDef.race.race.IsFlesh)
		{
			request.AllowGay = false;
		}
	}

	public static IEnumerable<CodeInstruction> TryGenerateNewPawnInternalTranspiler(IEnumerable<CodeInstruction> instructions)
	{
		bool done = false;
		MethodInfo allDefsInfo = AccessTools.PropertyGetter(typeof(DefDatabase<HeadTypeDef>), "AllDefs");
		List<CodeInstruction> instructionList = instructions.ToList();
		for (int i = 0; i < instructionList.Count; i++)
		{
			CodeInstruction instruction = instructionList[i];
			yield return instruction;
			if (!done && instruction.Calls(allDefsInfo))
			{
				done = true;
				yield return new CodeInstruction(OpCodes.Ldloc_0);
				yield return new CodeInstruction(instructionList[i - 2]);
				yield return CodeInstruction.Call(patchType, "HeadTypeFilter");
			}
		}
	}

	public static IEnumerable<HeadTypeDef> HeadTypeFilter(IEnumerable<HeadTypeDef> headTypes, Pawn pawn)
	{
		if (!(pawn.def is ThingDef_AlienRace alienProps))
		{
			return headTypes;
		}
		return headTypes.Intersect(alienProps.alienRace.generalSettings.alienPartGenerator.HeadTypes);
	}

	public static void GenerateSkillsPostfix(Pawn pawn)
	{
		foreach (BackstoryDef backstory in pawn.story.AllBackstories)
		{
			if (!(backstory is AlienBackstoryDef alienBackstory))
			{
				continue;
			}
			IEnumerable<SkillGain> passions = alienBackstory.passions;
			if (pawn.def is ThingDef_AlienRace alienProps)
			{
				passions = passions.Concat(alienProps.alienRace.generalSettings.passions);
			}
			foreach (SkillGain passion in passions)
			{
				pawn.skills.GetSkill(passion.skill).passion = (Passion)passion.amount;
			}
		}
	}

	public static IEnumerable<CodeInstruction> GenerateRandomMinTicksToNextLovinTranspiler(IEnumerable<CodeInstruction> instructions)
	{
		FieldInfo ageCurveInfo = AccessTools.Field(typeof(JobDriver_Lovin), "LovinIntervalHoursFromAgeCurve");
		foreach (CodeInstruction instruction in instructions)
		{
			yield return instruction;
			if (instruction.LoadsField(ageCurveInfo))
			{
				yield return new CodeInstruction(OpCodes.Ldarg_1);
				yield return CodeInstruction.Call(patchType, "LovinInterval");
			}
		}
	}

	public static SimpleCurve LovinInterval(SimpleCurve humanDefault, Pawn pawn)
	{
		if (!(pawn.def is ThingDef_AlienRace alienProps))
		{
			return humanDefault;
		}
		return alienProps.alienRace.generalSettings.lovinIntervalHoursFromAge ?? humanDefault;
	}

	public static IEnumerable<CodeInstruction> UnwillingWillingToDoGenderedTranspiler(IEnumerable<CodeInstruction> instructions)
	{
		List<CodeInstruction> instructionList = instructions.ToList();
		for (int i = 0; i < instructionList.Count; i++)
		{
			CodeInstruction instruction = instructionList[i];
			yield return instruction;
			if (instruction.opcode == OpCodes.Ldloc_0)
			{
				yield return CodeInstruction.LoadField(typeof(Pawn), "def");
				yield return CodeInstruction.LoadField(typeof(ThingDef), "race");
				yield return CodeInstruction.LoadField(typeof(RaceProperties), "hasGenders");
				yield return new CodeInstruction(OpCodes.Brfalse, instructionList[i + 4].operand);
				yield return new CodeInstruction(instruction);
			}
		}
	}

	public static void FoodUtilityAddThoughtsFromIdeoPrefix(ref HistoryEventDef eventDef, Pawn ingester, ThingDef foodDef, MeatSourceCategory meatSourceCategory)
	{
		if (meatSourceCategory == MeatSourceCategory.Humanlike && (foodDef.IsCorpse || foodDef.IsMeat) && Utilities.DifferentRace(ingester.def, foodDef.ingestible.sourceDef))
		{
			eventDef = AlienDefOf.HAR_AteAlienMeat;
		}
	}

	public static void SetupLastHumanMeatTickPrefix(Pawn ___pawn)
	{
		AlienPartGenerator.AlienComp alienComp = ___pawn.GetComp<AlienPartGenerator.AlienComp>();
		if (alienComp != null)
		{
			alienComp.lastAlienMeatIngestedTick = Find.TickManager.TicksGame;
			alienComp.lastAlienMeatIngestedTick -= new IntRange(0, 60000).RandomInRange;
		}
	}

	public static IEnumerable<CodeInstruction> DrawTradeableRowTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
	{
		MethodInfo doerWillingInfo = AccessTools.Method(typeof(IdeoUtility), "DoerWillingToDo", new Type[1] { typeof(HistoryEvent) });
		List<CodeInstruction> instructionList = instructions.ToList();
		Label skip = ilg.DefineLabel();
		for (int i = 0; i < instructionList.Count; i++)
		{
			CodeInstruction instruction = instructionList[i];
			yield return instruction;
			if (i > 5 && instruction.Calls(doerWillingInfo))
			{
				yield return new CodeInstruction(OpCodes.Dup);
				yield return new CodeInstruction(OpCodes.Brtrue, skip);
				yield return new CodeInstruction(OpCodes.Pop);
				yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TradeSession), "playerNegotiator"));
				yield return new CodeInstruction(OpCodes.Ldarg_1);
				yield return CodeInstruction.Call(patchType, "DrawTransferableRowIsWilling");
				yield return new CodeInstruction(OpCodes.Ldc_I4_0);
				yield return new CodeInstruction(OpCodes.Ceq);
				yield return new CodeInstruction(OpCodes.Nop)
				{
					labels = new List<Label>(1) { skip }
				};
			}
		}
	}

	public static bool DrawTransferableRowIsWilling(Pawn doer, Tradeable trad)
	{
		if (trad is Tradeable_Pawn && trad.AnyThing is Pawn)
		{
			return IdeoUtility.DoerWillingToDo(AlienDefOf.HAR_Alien_SoldSlave, doer);
		}
		return false;
	}

	public static IEnumerable<CodeInstruction> TradeablePawnResolveTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
	{
		MethodInfo sellingSlaveryInfo = AccessTools.Method(typeof(GuestUtility), "IsSellingToSlavery");
		MethodInfo buyingSlaveryInfo = AccessTools.Method(typeof(ITrader), "GiveSoldThingToPlayer");
		Label orbitalTradeLabel = ilg.DefineLabel();
		foreach (CodeInstruction instruction in instructions)
		{
			yield return instruction;
			if (instruction.Calls(sellingSlaveryInfo))
			{
				yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TradeSession), "playerNegotiator"));
				yield return new CodeInstruction(OpCodes.Ldloc_0);
				yield return new CodeInstruction(OpCodes.Ldloc_1);
				yield return CodeInstruction.Call(typeof(List<>).MakeGenericType(typeof(Pawn)), "get_Item");
				yield return CodeInstruction.Call(patchType, "SoldSlave");
			}
			if (instruction.Calls(buyingSlaveryInfo))
			{
				yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TradeSession), "trader"));
				yield return new CodeInstruction(OpCodes.Isinst, typeof(Pawn));
				yield return new CodeInstruction(OpCodes.Brfalse, orbitalTradeLabel);
				yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TradeSession), "trader"));
				yield return new CodeInstruction(OpCodes.Castclass, typeof(Pawn));
				yield return new CodeInstruction(OpCodes.Ldloc_S, 2);
				yield return new CodeInstruction(OpCodes.Ldloc_S, 3);
				yield return CodeInstruction.Call(typeof(List<>).MakeGenericType(typeof(Pawn)), "get_Item");
				yield return CodeInstruction.Call(patchType, "SoldSlave");
				yield return new CodeInstruction(OpCodes.Nop).WithLabels(orbitalTradeLabel);
			}
		}
	}

	public static void SoldSlave(Pawn pawn, Pawn slave)
	{
		if (Utilities.DifferentRace(pawn.def, slave.def) && ModsConfig.IdeologyActive)
		{
			Find.HistoryEventsManager.RecordEvent(new HistoryEvent(AlienDefOf.HAR_Alien_SoldSlave, pawn.Named(HistoryEventArgsNames.Doer), slave.Named(HistoryEventArgsNames.Victim)));
		}
	}

	public static void WillingToShareBedPostfix(Pawn pawn1, Pawn pawn2, ref bool __result)
	{
		if (Utilities.DifferentRace(pawn1.def, pawn2.def) && (!IdeoUtility.DoerWillingToDo(AlienDefOf.HAR_AlienDating_SharedBed, pawn1) || !IdeoUtility.DoerWillingToDo(AlienDefOf.HAR_AlienDating_SharedBed, pawn2)))
		{
			__result = false;
		}
	}

	public static void RomanceAttemptSuccessChancePostfix(Pawn initiator, Pawn recipient, ref float __result)
	{
		if (Utilities.DifferentRace(initiator.def, recipient.def) && (!IdeoUtility.DoerWillingToDo(AlienDefOf.HAR_AlienDating_BeginRomance, initiator) || !IdeoUtility.DoerWillingToDo(AlienDefOf.HAR_AlienDating_BeginRomance, recipient)))
		{
			__result = -1f;
		}
	}

	public static IEnumerable<CodeInstruction> RomanceAttemptInteractTranspiler(IEnumerable<CodeInstruction> instructions)
	{
		FieldInfo becameLoverInfo = AccessTools.Field(typeof(TaleDefOf), "BecameLover");
		foreach (CodeInstruction instruction in instructions)
		{
			if (instruction.LoadsField(becameLoverInfo))
			{
				yield return new CodeInstruction(OpCodes.Ldarg_1);
				yield return new CodeInstruction(OpCodes.Ldarg_2);
				yield return CodeInstruction.Call(patchType, "NewLoverHelper");
			}
			yield return instruction;
		}
	}

	public static void NewLoverHelper(Pawn initiator, Pawn recipient)
	{
		if (Utilities.DifferentRace(initiator.def, recipient.def) && ModsConfig.IdeologyActive)
		{
			Find.HistoryEventsManager.RecordEvent(new HistoryEvent(AlienDefOf.HAR_AlienDating_Dating, initiator.Named(HistoryEventArgsNames.Doer), recipient.Named(HistoryEventArgsNames.Victim)));
			Find.HistoryEventsManager.RecordEvent(new HistoryEvent(AlienDefOf.HAR_AlienDating_Dating, recipient.Named(HistoryEventArgsNames.Doer), initiator.Named(HistoryEventArgsNames.Victim)));
		}
	}

	public static void IngestedPrefix(Pawn ingester, Thing __instance)
	{
		if (__instance.Destroyed || !__instance.IngestibleNow || !FoodUtility.IsHumanlikeCorpseOrHumanlikeMeatOrIngredient(__instance))
		{
			return;
		}
		bool alienMeat = (__instance.def.IsCorpse && Utilities.DifferentRace(ingester.def, (__instance as Corpse).InnerPawn.def)) || (__instance.def.IsIngestible && __instance.def.IsMeat && Utilities.DifferentRace(ingester.def, __instance.def.ingestible.sourceDef));
		CompIngredients compIngredients = __instance.TryGetComp<CompIngredients>();
		if (compIngredients != null)
		{
			foreach (ThingDef ingredient in compIngredients.ingredients)
			{
				if (ingredient.IsMeat && Utilities.DifferentRace(ingester.def, ingredient.ingestible.sourceDef))
				{
					alienMeat = true;
				}
			}
		}
		if (ModsConfig.IdeologyActive)
		{
			Find.HistoryEventsManager.RecordEvent(new HistoryEvent(alienMeat ? AlienDefOf.HAR_AteAlienMeat : AlienDefOf.HAR_AteNonAlienFood, ingester.Named(HistoryEventArgsNames.Doer)));
		}
		if (alienMeat)
		{
			AlienPartGenerator.AlienComp alienComp = ingester.GetComp<AlienPartGenerator.AlienComp>();
			if (alienComp != null)
			{
				alienComp.lastAlienMeatIngestedTick = Find.TickManager.TicksGame;
			}
		}
	}

	public static IEnumerable<CodeInstruction> WoundWriteCacheTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
	{
		MethodInfo defaultAnchorInfo = AccessTools.Method(typeof(PawnWoundDrawer), "GetDefaultAnchor");
		List<CodeInstruction> instructionList = instructions.ToList();
		Label label = ilg.DefineLabel();
		for (int i = 0; i < instructionList.Count; i++)
		{
			CodeInstruction instruction = instructionList[i];
			if (instruction.opcode == OpCodes.Ldarg_0 && instructionList[i + 4].Calls(defaultAnchorInfo))
			{
				yield return new CodeInstruction(OpCodes.Ldarg_3).MoveLabelsFrom(instruction);
				yield return new CodeInstruction(OpCodes.Brfalse, label);
				instructionList[i + 5].WithLabels(label);
			}
			yield return instruction;
		}
	}

	public static void TotalStyleItemLikelihoodPostfix(ref float __result)
	{
		__result += float.Epsilon;
	}

	public static IEnumerable<CodeInstruction> KnowsMemoryThoughtTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
	{
		FieldInfo thoughtInfo = AccessTools.Field(typeof(PreceptComp_Thought), "thought");
		LocalBuilder thoughtLocal = ilg.DeclareLocal(typeof(ThoughtDef));
		foreach (CodeInstruction instruction in instructions)
		{
			yield return instruction;
			if (instruction.opcode == OpCodes.Stloc_0)
			{
				yield return new CodeInstruction(OpCodes.Ldarg_0);
				yield return new CodeInstruction(OpCodes.Ldfld, thoughtInfo);
				yield return new CodeInstruction(OpCodes.Ldarg_0);
				yield return new CodeInstruction(OpCodes.Ldarg_1);
				yield return new CodeInstruction(OpCodes.Ldarg_2);
				yield return CodeInstruction.Call(patchType, "KnowsGetHistoryEventThoughtDefReplacer");
				yield return new CodeInstruction(OpCodes.Stloc, thoughtLocal.LocalIndex);
			}
			if (instruction.LoadsField(thoughtInfo))
			{
				yield return new CodeInstruction(OpCodes.Pop);
				yield return new CodeInstruction(OpCodes.Ldloc, thoughtLocal.LocalIndex);
			}
		}
	}

	public static ThoughtDef KnowsGetHistoryEventThoughtDefReplacer(ThoughtDef thought, PreceptComp_KnowsMemoryThought comp, HistoryEvent ev, Precept precept)
	{
		ThoughtDef result = thought;
		ev.args.TryGetArg(HistoryEventArgsNames.Doer, out Pawn doer);
		ev.args.TryGetArg(HistoryEventArgsNames.Victim, out Pawn victim);
		if (thought == AlienDefOf.KnowButcheredHumanlikeCorpse)
		{
			if (Utilities.DifferentRace(doer.def, victim.def) && ModsConfig.IdeologyActive)
			{
				Find.HistoryEventsManager.RecordEvent(new HistoryEvent(AlienDefOf.HAR_ButcheredAlien, doer.Named(HistoryEventArgsNames.Doer), victim.Named(HistoryEventArgsNames.Victim)));
			}
			if (doer.def is ThingDef_AlienRace alienPropsPawn)
			{
				result = alienPropsPawn.alienRace.thoughtSettings.butcherThoughtSpecific?.FirstOrDefault((ButcherThought bt) => bt.raceList?.Contains(victim.def) ?? false)?.knowThought ?? alienPropsPawn.alienRace.thoughtSettings.butcherThoughtGeneral.knowThought;
			}
		}
		return result;
	}

	public static IEnumerable<CodeInstruction> SelfTookMemoryThoughtTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
	{
		FieldInfo thoughtInfo = AccessTools.Field(typeof(PreceptComp_Thought), "thought");
		LocalBuilder thoughtLocal = ilg.DeclareLocal(typeof(ThoughtDef));
		bool first = true;
		foreach (CodeInstruction instruction in instructions)
		{
			yield return instruction;
			if (instruction.LoadsField(thoughtInfo))
			{
				if (first)
				{
					first = false;
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Ldarg_1);
					yield return new CodeInstruction(OpCodes.Ldarg_2);
					yield return CodeInstruction.Call(patchType, "SelfTookGetHistoryEventThoughtDefReplacer");
					yield return new CodeInstruction(OpCodes.Stloc, thoughtLocal.LocalIndex);
				}
				else
				{
					yield return new CodeInstruction(OpCodes.Pop);
				}
				yield return new CodeInstruction(OpCodes.Ldloc, thoughtLocal.LocalIndex);
			}
		}
	}

	public static ThoughtDef SelfTookGetHistoryEventThoughtDefReplacer(ThoughtDef thought, PreceptComp_SelfTookMemoryThought comp, HistoryEvent ev, Precept precept)
	{
		ThoughtDef result = thought;
		Pawn doer = ev.args.GetArg<Pawn>(HistoryEventArgsNames.Doer);
		ev.args.TryGetArg(HistoryEventArgsNames.Victim, out Pawn victim);
		if (thought == AlienDefOf.ButcheredHumanlikeCorpse && doer.def is ThingDef_AlienRace alienPropsButcher)
		{
			result = alienPropsButcher.alienRace.thoughtSettings.butcherThoughtSpecific?.FirstOrDefault((ButcherThought bt) => bt.raceList?.Contains(victim.def) ?? false)?.thought ?? alienPropsButcher.alienRace.thoughtSettings.butcherThoughtGeneral.thought;
		}
		return result;
	}

	public static bool WantsToUseStylePrefix(Pawn pawn, StyleItemDef styleItemDef, ref bool __result)
	{
		if (!(pawn.def is ThingDef_AlienRace alienProps) || styleItemDef == null)
		{
			return true;
		}
		if (alienProps.alienRace.styleSettings[styleItemDef.GetType()].hasStyle)
		{
			if (!alienProps.alienRace.styleSettings[styleItemDef.GetType()].styleTagsOverride.NullOrEmpty())
			{
				__result = alienProps.alienRace.styleSettings[styleItemDef.GetType()].IsValidStyle(styleItemDef, pawn, useOverrides: true);
				return false;
			}
			return true;
		}
		__result = true;
		return false;
	}

	public static void WantsToUseStylePostfix(Pawn pawn, StyleItemDef styleItemDef, ref bool __result)
	{
		if (__result && pawn.def is ThingDef_AlienRace alienProps && styleItemDef != null)
		{
			__result = alienProps.alienRace.styleSettings[styleItemDef.GetType()].IsValidStyle(styleItemDef, pawn);
		}
	}

	public static void CacheRenderPawnPrefix(Pawn pawn, ref float cameraZoom, bool portrait)
	{
		if (!portrait)
		{
			cameraZoom /= (pawn.def as ThingDef_AlienRace)?.alienRace.generalSettings.alienPartGenerator.borderScale ?? 1f;
		}
	}

	public static void GlobalTextureAtlasGetFrameSetPrefix(Pawn pawn)
	{
		createPawnAtlasPawn = pawn;
	}

	public static IEnumerable<CodeInstruction> PawnTextureAtlasGetFrameSetTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
	{
		List<CodeInstruction> instructionList = instructions.ToList();
		bool done = false;
		Label jumpLabel = ilg.DefineLabel();
		for (int i = 0; i < instructionList.Count; i++)
		{
			CodeInstruction instruction = instructionList[i];
			if (!done && instruction.opcode == OpCodes.Stind_I1)
			{
				done = true;
				yield return new CodeInstruction(OpCodes.Ldarg_0)
				{
					labels = instruction.ExtractLabels()
				};
				yield return new CodeInstruction(OpCodes.Ldarg_1);
				yield return new CodeInstruction(OpCodes.Ldarg_0);
				yield return CodeInstruction.LoadField(typeof(PawnTextureAtlas), "freeFrameSets");
				yield return CodeInstruction.Call(patchType, "TextureAtlasSameRace");
				yield return new CodeInstruction(OpCodes.Brtrue_S, jumpLabel);
				yield return new CodeInstruction(OpCodes.Pop);
				yield return new CodeInstruction(OpCodes.Pop);
				yield return new CodeInstruction(OpCodes.Ldc_I4_0);
				yield return new CodeInstruction(OpCodes.Ret);
				instruction = instruction.WithLabels(jumpLabel);
			}
			yield return instruction;
		}
		if (!done)
		{
			Log.Error("PawnTextureAtlasGetFrameSetTranspiler failed to find entry point");
		}
	}

	public static bool TextureAtlasSameRace(PawnTextureAtlas atlas, Pawn pawn, List<PawnTextureAtlasFrameSet> frameSets)
	{
		Dictionary<Pawn, PawnTextureAtlasFrameSet>.KeyCollection keys = CachedData.pawnTextureAtlasFrameAssignments(atlas).Keys;
		int atlasScale = (pawn.def as ThingDef_AlienRace)?.alienRace.generalSettings.alienPartGenerator.atlasScale ?? 1;
		float borderScale = (pawn.def as ThingDef_AlienRace)?.alienRace.generalSettings.alienPartGenerator.borderScale ?? 1f;
		if (keys.Count == 0)
		{
			if (atlas.RawTexture.width == 2048 * atlasScale && Math.Abs(frameSets.First().meshes.First().vertices.First().x + borderScale) < 0.01f)
			{
				return true;
			}
		}
		else if (keys.Any((Pawn p) => p.def == pawn.def || (((p.def as ThingDef_AlienRace)?.alienRace.generalSettings.alienPartGenerator.atlasScale ?? 1) == atlasScale && (double)Math.Abs(((p.def as ThingDef_AlienRace)?.alienRace.generalSettings.alienPartGenerator.borderScale ?? 1f) - borderScale) < 0.01)))
		{
			return true;
		}
		return false;
	}

	public static float GetBorderSizeForPawn()
	{
		return (createPawnAtlasPawn.def as ThingDef_AlienRace)?.alienRace.generalSettings.alienPartGenerator.borderScale ?? 1f;
	}

	public static IEnumerable<CodeInstruction> PawnTextureAtlasConstructorFuncTranspiler(IEnumerable<CodeInstruction> instructions)
	{
		foreach (CodeInstruction instruction in instructions)
		{
			if (instruction.opcode == OpCodes.Ldc_R4)
			{
				yield return CodeInstruction.Call(patchType, "GetBorderSizeForPawn");
			}
			else
			{
				yield return instruction;
			}
		}
	}

	public static int GetAtlasSizeForPawn()
	{
		return (createPawnAtlasPawn.def as ThingDef_AlienRace)?.alienRace.generalSettings.alienPartGenerator.atlasScale ?? 1;
	}

	public static IEnumerable<CodeInstruction> PawnTextureAtlasConstructorTranspiler(IEnumerable<CodeInstruction> instructions)
	{
		foreach (CodeInstruction instruction in instructions)
		{
			yield return instruction;
			if (instruction.OperandIs(2048) || instruction.OperandIs(2048f))
			{
				if (instruction.opcode == OpCodes.Ldc_I4)
				{
					yield return CodeInstruction.Call(patchType, "GetAtlasSizeForPawn");
					yield return new CodeInstruction(OpCodes.Mul);
				}
				else if (instruction.opcode == OpCodes.Ldc_R4)
				{
					yield return CodeInstruction.Call(patchType, "GetAtlasSizeForPawn");
					yield return new CodeInstruction(OpCodes.Conv_R4);
					yield return new CodeInstruction(OpCodes.Mul);
				}
			}
			else if (instruction.OperandIs(128))
			{
				yield return CodeInstruction.Call(patchType, "GetAtlasSizeForPawn");
				yield return new CodeInstruction(OpCodes.Mul);
			}
		}
	}

	public static void CalcAnchorDataPostfix(Pawn pawn, BodyTypeDef.WoundAnchor anchor, ref Vector3 anchorOffset)
	{
		if (!(pawn.def is ThingDef_AlienRace alienRace))
		{
			return;
		}
		List<AlienPartGenerator.WoundAnchorReplacement> anchorReplacements = alienRace.alienRace.generalSettings.alienPartGenerator.anchorReplacements;
		foreach (AlienPartGenerator.WoundAnchorReplacement anchorReplacement in anchorReplacements)
		{
			if (anchorReplacement.ValidReplacement(anchor) && anchorReplacement.offsets != null)
			{
				anchorOffset = anchorReplacement.offsets.GetOffset(anchor.rotation.Value).GetOffset(portrait: false, pawn.story.bodyType, pawn.story.headType);
				break;
			}
		}
	}

	public static IEnumerable<BodyTypeDef.WoundAnchor> FindAnchorsPostfix(IEnumerable<BodyTypeDef.WoundAnchor> __result, Pawn pawn)
	{
		if (pawn.def is ThingDef_AlienRace alienRace)
		{
			List<AlienPartGenerator.WoundAnchorReplacement> anchorReplacements = alienRace.alienRace.generalSettings.alienPartGenerator.anchorReplacements;
			List<BodyTypeDef.WoundAnchor> result = new List<BodyTypeDef.WoundAnchor>();
			if (!__result.Any())
			{
				return Array.Empty<BodyTypeDef.WoundAnchor>();
			}
			{
				foreach (BodyTypeDef.WoundAnchor anchor in __result)
				{
					AlienPartGenerator.WoundAnchorReplacement replacement = anchorReplacements.FirstOrDefault((AlienPartGenerator.WoundAnchorReplacement war) => war.ValidReplacement(anchor));
					result.Add((replacement != null) ? replacement.replacement : anchor);
				}
				return result;
			}
		}
		return __result;
	}

	public static void ThoughtReplacementPrefix(MemoryThoughtHandler __instance, ref ThoughtDef def)
	{
		Pawn pawn = __instance.pawn;
		if (pawn.def is ThingDef_AlienRace race)
		{
			def = race.alienRace.thoughtSettings.ReplaceIfApplicable(def);
		}
	}

	public static IEnumerable<CodeInstruction> MinAgeForAdulthood(IEnumerable<CodeInstruction> instructions)
	{
		float value = (float)AccessTools.Field(typeof(PawnBioAndNameGenerator), "MinAgeForAdulthood").GetValue(null);
		foreach (CodeInstruction instruction in instructions)
		{
			if (instruction.opcode == OpCodes.Ldc_R4 && instruction.OperandIs(value))
			{
				yield return new CodeInstruction(OpCodes.Ldarg_0);
				yield return instruction;
				yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(patchType, "GetMinAgeForAdulthood"));
			}
			else
			{
				yield return instruction;
			}
		}
	}

	public static float GetMinAgeForAdulthood(Pawn pawn, float value)
	{
		if (!(pawn.def is ThingDef_AlienRace alienProps))
		{
			return value;
		}
		return alienProps.alienRace.generalSettings.minAgeForAdulthood;
	}

	public static IEnumerable<CodeInstruction> MisandryMisogynyTranspiler(IEnumerable<CodeInstruction> instructions)
	{
		List<CodeInstruction> instructionList = instructions.ToList();
		FieldInfo defInfo = AccessTools.Field(typeof(Thing), "def");
		bool yield = true;
		foreach (CodeInstruction instruction in instructionList)
		{
			if (yield && instruction.OperandIs(defInfo))
			{
				yield = false;
			}
			if (yield)
			{
				yield return instruction;
			}
			else if (instruction.opcode == OpCodes.Ldarg_2)
			{
				yield = true;
			}
		}
	}

	public static IEnumerable<CodeInstruction> PostureTranspiler(IEnumerable<CodeInstruction> instructions)
	{
		MethodInfo postureInfo = AccessTools.Method(typeof(PawnUtility), "GetPosture");
		List<CodeInstruction> instructionList = instructions.ToList();
		foreach (CodeInstruction instruction in instructionList)
		{
			bool found = instruction.Calls(postureInfo);
			if (found)
			{
				yield return new CodeInstruction(OpCodes.Dup);
			}
			yield return instruction;
			if (found)
			{
				yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(patchType, "PostureTweak"));
			}
		}
	}

	public static PawnPosture PostureTweak(Pawn pawn, PawnPosture posture)
	{
		if (posture != PawnPosture.Standing && pawn.def is ThingDef_AlienRace alienProps && !alienProps.alienRace.generalSettings.canLayDown)
		{
			Building_Bed building_Bed = pawn.CurrentBed();
			if (building_Bed == null || !building_Bed.def.defName.EqualsIgnoreCase("ET_Bed"))
			{
				return PawnPosture.Standing;
			}
		}
		return posture;
	}

	public static IEnumerable<CodeInstruction> BodyReferenceTranspiler(IEnumerable<CodeInstruction> instructions)
	{
		FieldInfo bodyInfo = AccessTools.Field(typeof(RaceProperties), "body");
		FieldInfo propsInfo = AccessTools.Field(typeof(ThingDef), "race");
		FieldInfo defInfo = AccessTools.Field(typeof(Thing), "def");
		MethodInfo raceprops = AccessTools.Property(typeof(Pawn), "RaceProps").GetGetMethod();
		List<CodeInstruction> instructionList = instructions.ToList();
		for (int i = 0; i < instructionList.Count; i++)
		{
			CodeInstruction instruction = instructionList[i];
			if (i < instructionList.Count - 2 && instructionList[i + 2].OperandIs(bodyInfo) && instructionList[i + 1].OperandIs(propsInfo) && instruction.OperandIs(defInfo))
			{
				instruction.opcode = OpCodes.Call;
				instruction.operand = AccessTools.Method(patchType, "ReplacedBody");
				i += 2;
			}
			if (i < instructionList.Count - 1 && instructionList[i + 1].OperandIs(bodyInfo) && instruction.OperandIs(defInfo))
			{
				instruction.opcode = OpCodes.Call;
				instruction.operand = AccessTools.Method(patchType, "ReplacedBody");
				i++;
			}
			if (i < instructionList.Count - 1 && instructionList[i + 1].OperandIs(bodyInfo) && instruction.OperandIs(raceprops))
			{
				instruction.opcode = OpCodes.Call;
				instruction.operand = AccessTools.Method(patchType, "ReplacedBody");
				i++;
			}
			yield return instruction;
		}
	}

	public static BodyDef ReplacedBody(Pawn pawn)
	{
		if (!(pawn.def is ThingDef_AlienRace))
		{
			return pawn.RaceProps.body;
		}
		return (pawn.ageTracker?.CurLifeStageRace as LifeStageAgeAlien)?.body ?? pawn.RaceProps.body;
	}

	public static bool ChangeKindPrefix(Pawn __instance, ref PawnKindDef newKindDef)
	{
		if (!__instance.RaceProps.Humanlike || !newKindDef.RaceProps.Humanlike || newKindDef == PawnKindDefOf.WildMan)
		{
			return true;
		}
		if (__instance.kindDef == PawnKindDefOf.WildMan)
		{
			PawnKindDef originalKind = __instance.GetComp<AlienPartGenerator.AlienComp>()?.originalKindDef;
			newKindDef = ((originalKind != PawnKindDefOf.WildMan) ? (originalKind ?? newKindDef) : newKindDef);
			return true;
		}
		return false;
	}

	public static void GenerateGearForPostfix(Pawn pawn)
	{
		pawn.story?.AllBackstories?.OfType<AlienBackstoryDef>().SelectMany((AlienBackstoryDef bd) => bd.forcedItems).Concat(bioReference?.forcedItems ?? new List<ThingDefCountRangeClass>(0))
			.Do(delegate(ThingDefCountRangeClass tdcrc)
			{
				int num = tdcrc.countRange.RandomInRange;
				while (num > 0)
				{
					Thing thing = ThingMaker.MakeThing(tdcrc.thingDef, GenStuff.RandomStuffFor(tdcrc.thingDef));
					thing.stackCount = Mathf.Min(num, tdcrc.thingDef.stackLimit);
					num -= thing.stackCount;
					pawn.inventory?.TryAddItemNotForSale(thing);
				}
			});
	}

	public static void AddHediffPostfix(Pawn ___pawn, Hediff hediff)
	{
		if (!hediff.def.hairColorOverride.HasValue && !hediff.def.HasDefinedGraphicProperties)
		{
			AlienPartGenerator.AlienComp.RegenerateAddonGraphicsWithCondition(___pawn, new HashSet<Type> { typeof(ConditionHediff) });
		}
	}

	public static void RemoveHediffPostfix(Pawn ___pawn, Hediff hediff)
	{
		if (!hediff.def.HasDefinedGraphicProperties && !hediff.def.forceRenderTreeRecache)
		{
			AlienPartGenerator.AlienComp.RegenerateAddonGraphicsWithCondition(___pawn, new HashSet<Type> { typeof(ConditionHediff) });
		}
	}

	public static void HediffChangedPostfix(Pawn ___pawn, HediffSet ___hediffSet)
	{
		if (Current.ProgramState == ProgramState.Playing && ___pawn.Spawned && ___pawn.def is ThingDef_AlienRace)
		{
			AlienPartGenerator.AlienComp.RegenerateAddonGraphicsWithCondition(___pawn, new HashSet<Type> { typeof(ConditionHediffSeverity) });
		}
	}

	public static IEnumerable<CodeInstruction> BaseHeadOffsetAtTranspiler(IEnumerable<CodeInstruction> instructions)
	{
		FieldInfo offsetInfo = AccessTools.Field(typeof(BodyTypeDef), "headOffset");
		foreach (CodeInstruction instruction in instructions)
		{
			yield return instruction;
			if (instruction.OperandIs(offsetInfo))
			{
				yield return new CodeInstruction(OpCodes.Ldarg_0);
				yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(PawnRenderer), "pawn"));
				yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(patchType, "BaseHeadOffsetAtHelper"));
			}
		}
	}

	public static Vector2 BaseHeadOffsetAtHelper(Vector2 offset, Pawn pawn)
	{
		Vector2 vector;
		if (!(pawn.ageTracker.CurLifeStageRace is LifeStageAgeAlien ageAlien))
		{
			vector = Vector2.zero;
		}
		else
		{
			Gender gender = pawn.gender;
			Vector2 vector2 = ((gender != Gender.Female) ? ageAlien.headOffset : ageAlien.headFemaleOffset);
			vector = vector2;
		}
		Vector2 alienHeadOffset = vector;
		return offset + alienHeadOffset;
	}

	public static void BaseHeadOffsetAtPostfix(ref Vector3 __result, Rot4 rotation, Pawn ___pawn)
	{
		LifeStageAgeAlien stageAgeAlien = ___pawn.ageTracker.CurLifeStageRace as LifeStageAgeAlien;
		Vector3 offsetSpecific = ((___pawn.gender != Gender.Female) ? stageAgeAlien?.headOffsetDirectional : stageAgeAlien?.headFemaleOffsetDirectional)?.GetOffset(rotation)?.GetOffset(portrait: false, ___pawn.story.bodyType, ___pawn.story.headType) ?? Vector3.zero;
		__result += offsetSpecific;
	}

	public static void CanInteractWithAnimalPostfix(ref bool __result, Pawn pawn, Pawn animal)
	{
		__result = __result && RaceRestrictionSettings.CanTame(animal.def, pawn.def);
	}

	public static void CanDesignateThingTamePostfix(Designator __instance, ref AcceptanceReport __result, Thing t)
	{
		if (__result.Accepted && __instance is Designator_Build)
		{
			__result = colonistRaces.Any((ThingDef td) => RaceRestrictionSettings.CanTame(t.def, td));
		}
	}

	public static IEnumerable<CodeInstruction> RecalculateLifeStageIndexTranspiler(IEnumerable<CodeInstruction> instructions)
	{
		MethodInfo biotechInfo = AccessTools.PropertyGetter(typeof(ModsConfig), "BiotechActive");
		foreach (CodeInstruction instruction in instructions)
		{
			if (instruction.Calls(biotechInfo))
			{
				yield return new CodeInstruction(OpCodes.Ldc_I4_1);
			}
			else
			{
				yield return instruction;
			}
		}
	}

	public static void HasHeadPrefix(HediffSet __instance)
	{
		headPawnDef = (__instance.pawn.def as ThingDef_AlienRace)?.alienRace.generalSettings.alienPartGenerator.headBodyPartDef;
	}

	public static void HasHeadPostfix(BodyPartRecord x, ref bool __result)
	{
		__result = ((headPawnDef != null) ? (x.def == headPawnDef) : __result);
	}

	public static void GenerateInitialHediffsPostfix(Pawn pawn)
	{
		foreach (HediffDef hd in pawn.story?.AllBackstories?.OfType<AlienBackstoryDef>().SelectMany((AlienBackstoryDef bd) => bd.forcedHediffs).Concat(bioReference?.forcedHediffs ?? new List<HediffDef>(0)) ?? Array.Empty<HediffDef>())
		{
			BodyPartRecord bodyPartRecord = null;
			DefDatabase<RecipeDef>.AllDefs.FirstOrDefault((RecipeDef rd) => rd.addsHediff == hd)?.appliedOnFixedBodyParts.SelectMany((BodyPartDef bpd) => from bpr in pawn.health.hediffSet.GetNotMissingParts()
				where bpr.def == bpd && !pawn.health.hediffSet.hediffs.Any((Hediff h) => h.def == hd && h.Part == bpr)
				select bpr).TryRandomElement(out bodyPartRecord);
			pawn.health.AddHediff(hd, bodyPartRecord);
		}
	}

	public static void GenerateStartingApparelForPostfix()
	{
		CachedData.allApparelPairs().AddRange(apparelList);
	}

	public static void GenerateStartingApparelForPrefix(Pawn pawn)
	{
		apparelList = new HashSet<ThingStuffPair>();
		foreach (ThingStuffPair pair in CachedData.allApparelPairs().ListFullCopy())
		{
			ThingDef equipment = pair.thing;
			if (!RaceRestrictionSettings.CanWear(equipment, pawn.def))
			{
				apparelList.Add(pair);
			}
		}
		CachedData.allApparelPairs().RemoveAll((ThingStuffPair tsp) => apparelList.Contains(tsp));
	}

	public static IEnumerable<CodeInstruction> TryGenerateWeaponForTranspiler(IEnumerable<CodeInstruction> instructions)
	{
		List<CodeInstruction> instructionList = instructions.ToList();
		FieldInfo workingWeaponsInfo = AccessTools.Field(typeof(PawnWeaponGenerator), "workingWeapons");
		for (int i = 0; i < instructionList.Count; i++)
		{
			CodeInstruction instruction = instructionList[i];
			if (instruction.LoadsField(workingWeaponsInfo) && instructionList[i + 1].Calls(AccessTools.PropertyGetter(typeof(List<ThingStuffPair>), "Count")))
			{
				yield return CodeInstruction.Call(patchType, "TryGenerateWeaponForCleanup").MoveLabelsFrom(instruction);
			}
			yield return instruction;
			if (instruction.opcode == OpCodes.Brtrue_S && instructionList[i - 1].Calls(AccessTools.PropertyGetter(typeof(List<string>), "Count")) && instructionList[i - 2].LoadsField(AccessTools.Field(typeof(PawnKindDef), "weaponTags")))
			{
				yield return CodeInstruction.Call(patchType, "TryGenerateWeaponForCleanup").MoveLabelsFrom(instructionList[i + 1]);
			}
		}
	}

	public static void TryGenerateWeaponForCleanup()
	{
		if (weaponList.Count > 0)
		{
			CachedData.allWeaponPairs().AddRange(weaponList);
			weaponList.Clear();
		}
	}

	public static void TryGenerateWeaponForPrefix(Pawn pawn)
	{
		weaponList.Clear();
		foreach (ThingStuffPair pair in CachedData.allWeaponPairs().ListFullCopy())
		{
			ThingDef equipment = pair.thing;
			if (!RaceRestrictionSettings.CanEquip(equipment, pawn.def))
			{
				weaponList.Add(pair);
			}
		}
		CachedData.allWeaponPairs().RemoveAll((ThingStuffPair tsp) => weaponList.Contains(tsp));
	}

	public static void DamageInfosToApplyPostfix(Verb __instance, ref IEnumerable<DamageInfo> __result)
	{
		if (!__instance.CasterIsPawn)
		{
			return;
		}
		ThingDef def = __instance.CasterPawn.def;
		ThingDef_AlienRace alienProps = def as ThingDef_AlienRace;
		if (alienProps != null && __instance.CasterPawn.CurJob.def == JobDefOf.SocialFight)
		{
			__result = __result.Select((DamageInfo di) => new DamageInfo(di.Def, Math.Min(di.Amount, alienProps.alienRace.generalSettings.maxDamageForSocialfight), 0f, di.Angle, di.Instigator, di.HitPart, di.Weapon, di.Category));
		}
	}

	public static void CanEverEatPostfix(ref bool __result, RaceProperties __instance, ThingDef t)
	{
		if (__instance.Humanlike && __result)
		{
			__result = RaceRestrictionSettings.CanEat(t, CachedData.GetRaceFromRaceProps(__instance));
		}
	}

	public static IEnumerable<Rule> RulesForPawnPostfix(IEnumerable<Rule> __result, Pawn pawn, string pawnSymbol)
	{
		return __result.AddItem(new Rule_String(pawnSymbol + "_alienRace", pawn.def.LabelCap));
	}

	public static IEnumerable<CodeInstruction> GenerateTraitsForTranspiler(IEnumerable<CodeInstruction> instructions)
	{
		List<CodeInstruction> instructionList = instructions.ToList();
		MethodInfo defListInfo = AccessTools.Property(typeof(DefDatabase<TraitDef>), "AllDefsListForReading").GetGetMethod();
		FieldInfo growthMomentAgesInfo = AccessTools.Field(typeof(GrowthUtility), "GrowthMomentAges");
		foreach (CodeInstruction instruction in instructionList)
		{
			if (instruction.LoadsField(growthMomentAgesInfo))
			{
				yield return new CodeInstruction(OpCodes.Ldarg_0).MoveLabelsFrom(instruction);
				yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Pawn), "def"));
				yield return CodeInstruction.Call(patchType, "GrowthMomentHelper", new Type[1] { typeof(ThingDef) });
			}
			else
			{
				yield return instruction;
			}
			if (instruction.opcode == OpCodes.Call && instruction.OperandIs(defListInfo))
			{
				yield return new CodeInstruction(OpCodes.Ldarg_0);
				yield return CodeInstruction.Call(patchType, "GenerateTraitsValidator");
			}
		}
	}

	public static IEnumerable<CodeInstruction> GenerateTraitsTranspiler(IEnumerable<CodeInstruction> instructions)
	{
		List<CodeInstruction> instructionList = instructions.ToList();
		foreach (CodeInstruction instruction in instructionList)
		{
			if (instruction.opcode == OpCodes.Stloc_0)
			{
				yield return new CodeInstruction(OpCodes.Ldarg_0);
			}
			yield return instruction;
		}
	}

	public static IEnumerable<TraitDef> GenerateTraitsValidator(List<TraitDef> traits, Pawn p)
	{
		return traits.Where((TraitDef tr) => RaceRestrictionSettings.CanGetTrait(tr, p));
	}

	public static void AssigningCandidatesPostfix(ref IEnumerable<Pawn> __result, CompAssignableToPawn __instance)
	{
		__result = (__instance.parent.def.building.bed_humanlike ? __result.Where((Pawn p) => RestUtility.CanUseBedEver(p, __instance.parent.def)) : __result);
	}

	public static void CanUseBedEverPostfix(ref bool __result, Pawn p, ThingDef bedDef)
	{
		if (__result)
		{
			__result = !(p.def is ThingDef_AlienRace alienProps) || alienProps.alienRace.generalSettings.CanUseBed(bedDef);
		}
	}

	public static IEnumerable<CodeInstruction> GetTraderCaravanRoleTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
	{
		MethodInfo traderRoleInfo = AccessTools.Method(patchType, "GetTraderCaravanRoleInfix");
		foreach (CodeInstruction instruction in instructions)
		{
			if (instruction.opcode == OpCodes.Ldc_I4_3)
			{
				Label jumpToEnd = il.DefineLabel();
				yield return new CodeInstruction(OpCodes.Ldarg_0)
				{
					labels = instruction.labels.ListFullCopy()
				};
				instruction.labels.Clear();
				yield return new CodeInstruction(OpCodes.Call, traderRoleInfo);
				yield return new CodeInstruction(OpCodes.Brfalse_S, jumpToEnd);
				yield return new CodeInstruction(OpCodes.Ldc_I4_4);
				yield return new CodeInstruction(OpCodes.Ret);
				yield return new CodeInstruction(OpCodes.Nop)
				{
					labels = new List<Label> { jumpToEnd }
				};
			}
			yield return instruction;
		}
	}

	private static bool GetTraderCaravanRoleInfix(Pawn p)
	{
		if (p.def is ThingDef_AlienRace)
		{
			return DefDatabase<RaceSettings>.AllDefs.Any((RaceSettings rs) => rs.pawnKindSettings.alienslavekinds.Any((PawnKindEntry pke) => pke.kindDefs.Contains(p.kindDef)));
		}
		return false;
	}

	public static bool GetGenderSpecificLabelPrefix(Pawn pawn, ref string __result, PawnRelationDef __instance)
	{
		if (pawn.def is ThingDef_AlienRace alienProps)
		{
			RelationRenamer ren = alienProps.alienRace.relationSettings.renamer?.FirstOrDefault((RelationRenamer rn) => rn.relation == __instance);
			if (ren != null)
			{
				__result = ((pawn.gender == Gender.Female) ? ren.femaleLabel : ren.label);
				if (__result.CanTranslate())
				{
					__result = __result.Translate();
				}
				return false;
			}
		}
		return true;
	}

	public static bool GeneratePawnRelationsPrefix(Pawn pawn, ref PawnGenerationRequest request)
	{
		PawnGenerationRequest localReq = request;
		if (!pawn.RaceProps.Humanlike || pawn.RaceProps.hasGenders || !(pawn.def is ThingDef_AlienRace))
		{
			return true;
		}
		List<KeyValuePair<Pawn, PawnRelationDef>> list = new List<KeyValuePair<Pawn, PawnRelationDef>>();
		List<PawnRelationDef> allDefsListForReading = DefDatabase<PawnRelationDef>.AllDefsListForReading;
		List<Pawn> enumerable = PawnsFinder.AllMapsWorldAndTemporary_AliveOrDead.Where((Pawn x) => x.def == pawn.def).ToList();
		enumerable.ForEach(delegate(Pawn current)
		{
			if (current.Discarded)
			{
				Log.Warning(string.Concat("Warning during generating pawn relations for ", pawn, ": Pawn ", current, " is discarded, yet he was yielded by PawnUtility. Discarding a pawn means that he is no longer managed by anything."));
			}
			else
			{
				allDefsListForReading.ForEach(delegate(PawnRelationDef relationDef)
				{
					if (relationDef.generationChanceFactor > 0f)
					{
						list.Add(new KeyValuePair<Pawn, PawnRelationDef>(current, relationDef));
					}
				});
			}
		});
		KeyValuePair<Pawn, PawnRelationDef> keyValuePair = list.RandomElementByWeightWithDefault((KeyValuePair<Pawn, PawnRelationDef> x) => x.Value.familyByBloodRelation ? GenerationChanceGenderless(x.Value, pawn, x.Key, localReq) : 0f, 82f);
		Pawn other = keyValuePair.Key;
		if (other != null)
		{
			CreateRelationGenderless(keyValuePair.Value, pawn, other);
		}
		KeyValuePair<Pawn, PawnRelationDef> keyValuePair2 = list.RandomElementByWeightWithDefault((KeyValuePair<Pawn, PawnRelationDef> x) => (!x.Value.familyByBloodRelation) ? GenerationChanceGenderless(x.Value, pawn, x.Key, localReq) : 0f, 82f);
		other = keyValuePair2.Key;
		if (other != null)
		{
			CreateRelationGenderless(keyValuePair2.Value, pawn, other);
		}
		return false;
	}

	private static float GenerationChanceGenderless(PawnRelationDef relationDef, Pawn pawn, Pawn current, PawnGenerationRequest request)
	{
		float generationChance = relationDef.generationChanceFactor;
		float lifeExpectancy = pawn.RaceProps.lifeExpectancy;
		if (relationDef == PawnRelationDefOf.Child)
		{
			generationChance = ChanceOfBecomingGenderlessChildOf(current, pawn, current.relations.GetFirstDirectRelationPawn(PawnRelationDefOf.Parent, (Pawn p) => p != pawn));
			GenerationChanceChildPostfix(ref generationChance, pawn, current);
		}
		else if (relationDef == PawnRelationDefOf.ExLover)
		{
			generationChance = 0.2f;
			GenerationChanceExLoverPostfix(ref generationChance, pawn, current);
		}
		else if (relationDef == PawnRelationDefOf.ExSpouse)
		{
			generationChance = 0.2f;
			GenerationChanceExSpousePostfix(ref generationChance, pawn, current);
		}
		else if (relationDef == PawnRelationDefOf.Fiance)
		{
			generationChance = Mathf.Clamp(GenMath.LerpDouble(lifeExpectancy / 1.6f, lifeExpectancy, 1f, 0.01f, pawn.ageTracker.AgeBiologicalYearsFloat), 0.01f, 1f) * Mathf.Clamp(GenMath.LerpDouble(lifeExpectancy / 1.6f, lifeExpectancy, 1f, 0.01f, current.ageTracker.AgeBiologicalYearsFloat), 0.01f, 1f);
			if (LovePartnerRelationUtility.HasAnyLovePartner(pawn) || LovePartnerRelationUtility.HasAnyLovePartner(current))
			{
				generationChance = 0f;
			}
			GenerationChanceFiancePostfix(ref generationChance, pawn, current);
		}
		else if (relationDef == PawnRelationDefOf.Lover)
		{
			generationChance = 0.5f;
			if (LovePartnerRelationUtility.HasAnyLovePartner(pawn) || LovePartnerRelationUtility.HasAnyLovePartner(current))
			{
				generationChance = 0f;
			}
			GenerationChanceLoverPostfix(ref generationChance, pawn, current);
		}
		else if (relationDef == PawnRelationDefOf.Parent)
		{
			generationChance = ChanceOfBecomingGenderlessChildOf(current, pawn, current.relations.GetFirstDirectRelationPawn(PawnRelationDefOf.Parent, (Pawn p) => p != pawn));
			GenerationChanceParentPostfix(ref generationChance, pawn, current);
		}
		else if (relationDef == PawnRelationDefOf.Sibling)
		{
			generationChance = ChanceOfBecomingGenderlessChildOf(current, pawn, current.relations.GetFirstDirectRelationPawn(PawnRelationDefOf.Parent, (Pawn p) => p != pawn));
			generationChance *= 0.65f;
			GenerationChanceSiblingPostfix(ref generationChance, pawn, current);
		}
		else if (relationDef == PawnRelationDefOf.Spouse)
		{
			generationChance = 0.5f;
			if (LovePartnerRelationUtility.HasAnyLovePartner(pawn) || LovePartnerRelationUtility.HasAnyLovePartner(current))
			{
				generationChance = 0f;
			}
			GenerationChanceSpousePostfix(ref generationChance, pawn, current);
		}
		return generationChance * relationDef.Worker.BaseGenerationChanceFactor(pawn, current, request);
	}

	private static void CreateRelationGenderless(PawnRelationDef relationDef, Pawn pawn, Pawn other)
	{
		if (relationDef == PawnRelationDefOf.Child)
		{
			Pawn parent = other.relations.GetFirstDirectRelationPawn(PawnRelationDefOf.Parent);
			if (parent != null)
			{
				pawn.relations.AddDirectRelation((LovePartnerRelationUtility.HasAnyLovePartner(parent) || Rand.Value > 0.8f) ? PawnRelationDefOf.ExLover : PawnRelationDefOf.Spouse, parent);
			}
			other.relations.AddDirectRelation(PawnRelationDefOf.Parent, pawn);
		}
		if (relationDef == PawnRelationDefOf.ExLover)
		{
			if (!pawn.GetRelations(other).Contains(PawnRelationDefOf.ExLover))
			{
				pawn.relations.AddDirectRelation(PawnRelationDefOf.ExLover, other);
			}
			other.relations.Children.ToList().ForEach(delegate(Pawn p)
			{
				if (p.relations.DirectRelations.Count((DirectPawnRelation dpr) => dpr.def == PawnRelationDefOf.Parent) < 2 && (double)Rand.Value < 0.35)
				{
					p.relations.AddDirectRelation(PawnRelationDefOf.Parent, pawn);
				}
			});
		}
		if (relationDef == PawnRelationDefOf.ExSpouse)
		{
			pawn.relations.AddDirectRelation(PawnRelationDefOf.ExSpouse, other);
			other.relations.Children.ToList().ForEach(delegate(Pawn p)
			{
				if (p.relations.DirectRelations.Count((DirectPawnRelation dpr) => dpr.def == PawnRelationDefOf.Parent) < 2 && Rand.Value < 1f)
				{
					p.relations.AddDirectRelation(PawnRelationDefOf.Parent, pawn);
				}
			});
		}
		if (relationDef == PawnRelationDefOf.Fiance)
		{
			pawn.relations.AddDirectRelation(PawnRelationDefOf.Fiance, other);
			other.relations.Children.ToList().ForEach(delegate(Pawn p)
			{
				if (p.relations.DirectRelations.Count((DirectPawnRelation dpr) => dpr.def == PawnRelationDefOf.Parent) < 2 && (double)Rand.Value < 0.7)
				{
					p.relations.AddDirectRelation(PawnRelationDefOf.Parent, pawn);
				}
			});
		}
		if (relationDef == PawnRelationDefOf.Lover)
		{
			pawn.relations.AddDirectRelation(PawnRelationDefOf.Lover, other);
			other.relations.Children.ToList().ForEach(delegate(Pawn p)
			{
				if (p.relations.DirectRelations.Count((DirectPawnRelation dpr) => dpr.def == PawnRelationDefOf.Parent) < 2 && Rand.Value < 0.35f)
				{
					p.relations.AddDirectRelation(PawnRelationDefOf.Parent, pawn);
				}
			});
		}
		if (relationDef == PawnRelationDefOf.Parent)
		{
			Pawn parent2 = other.relations.GetFirstDirectRelationPawn(PawnRelationDefOf.Parent);
			if (parent2 != null && pawn != parent2 && !pawn.GetRelations(parent2).Contains(PawnRelationDefOf.ExLover))
			{
				pawn.relations.AddDirectRelation((LovePartnerRelationUtility.HasAnyLovePartner(parent2) || Rand.Value > 0.8f) ? PawnRelationDefOf.ExLover : PawnRelationDefOf.Spouse, parent2);
			}
			pawn.relations.AddDirectRelation(PawnRelationDefOf.Parent, other);
		}
		if (relationDef == PawnRelationDefOf.Sibling)
		{
			Pawn parent3 = other.relations.GetFirstDirectRelationPawn(PawnRelationDefOf.Parent);
			List<DirectPawnRelation> dprs = other.relations.DirectRelations.Where((DirectPawnRelation dpr) => dpr.def == PawnRelationDefOf.Parent && dpr.otherPawn != parent3).ToList();
			Pawn parent4 = (dprs.NullOrEmpty() ? null : dprs.First().otherPawn);
			if (parent3 == null)
			{
				parent3 = PawnGenerator.GeneratePawn(other.kindDef, Find.FactionManager.FirstFactionOfDef(other.kindDef.defaultFactionDef) ?? Find.FactionManager.AllFactions.RandomElement());
				if (!other.GetRelations(parent3).Contains(PawnRelationDefOf.Parent))
				{
					other.relations.AddDirectRelation(PawnRelationDefOf.Parent, parent3);
				}
			}
			if (parent4 == null)
			{
				parent4 = PawnGenerator.GeneratePawn(other.kindDef, Find.FactionManager.FirstFactionOfDef(other.kindDef.defaultFactionDef) ?? Find.FactionManager.AllFactions.RandomElement());
				if (!other.GetRelations(parent4).Contains(PawnRelationDefOf.Parent))
				{
					other.relations.AddDirectRelation(PawnRelationDefOf.Parent, parent4);
				}
			}
			if (!parent3.GetRelations(parent4).Any((PawnRelationDef prd) => prd == PawnRelationDefOf.ExLover || prd == PawnRelationDefOf.Lover))
			{
				parent3.relations.AddDirectRelation((LovePartnerRelationUtility.HasAnyLovePartner(parent3) || (double)Rand.Value > 0.8) ? PawnRelationDefOf.ExLover : PawnRelationDefOf.Lover, parent4);
			}
			if (!pawn.GetRelations(parent3).Contains(PawnRelationDefOf.Parent) && pawn != parent3)
			{
				pawn.relations.AddDirectRelation(PawnRelationDefOf.Parent, parent3);
			}
			if (!pawn.GetRelations(parent4).Contains(PawnRelationDefOf.Parent) && pawn != parent4)
			{
				pawn.relations.AddDirectRelation(PawnRelationDefOf.Parent, parent4);
			}
		}
		if (relationDef != PawnRelationDefOf.Spouse)
		{
			return;
		}
		if (!pawn.GetRelations(other).Contains(PawnRelationDefOf.Spouse))
		{
			pawn.relations.AddDirectRelation(PawnRelationDefOf.Spouse, other);
		}
		other.relations.Children.ToList().ForEach(delegate(Pawn p)
		{
			if (pawn != p && p.relations.DirectRelations.Count((DirectPawnRelation dpr) => dpr.def == PawnRelationDefOf.Parent) < 2 && p.relations.GetFirstDirectRelationPawn(PawnRelationDefOf.Parent, (Pawn x) => x == pawn) == null && (double)Rand.Value < 0.7)
			{
				p.relations.AddDirectRelation(PawnRelationDefOf.Parent, pawn);
			}
		});
	}

	private static float ChanceOfBecomingGenderlessChildOf(Pawn child, Pawn parent1, Pawn parent2)
	{
		if (child == null || parent1 == null || (parent2 != null && child.relations.DirectRelations.Count((DirectPawnRelation dpr) => dpr.def == PawnRelationDefOf.Parent) <= 1))
		{
			return 0f;
		}
		if (parent2 != null && !LovePartnerRelationUtility.LovePartnerRelationExists(parent1, parent2) && !LovePartnerRelationUtility.ExLovePartnerRelationExists(parent1, parent2))
		{
			return 0f;
		}
		float num2 = 1f;
		float num3 = 1f;
		Traverse childRelation = Traverse.Create(typeof(ChildRelationUtility));
		float num4 = childRelation.Method("GetParentAgeFactor", parent1, child, parent1.RaceProps.lifeExpectancy / 5f, parent1.RaceProps.lifeExpectancy / 2.5f, parent1.RaceProps.lifeExpectancy / 1.6f).GetValue<float>();
		if (Math.Abs(num4) < 0.001f)
		{
			return 0f;
		}
		if (parent2 != null)
		{
			num2 = childRelation.Method("GetParentAgeFactor", parent2, child, parent1.RaceProps.lifeExpectancy / 5f, parent1.RaceProps.lifeExpectancy / 2.5f, parent1.RaceProps.lifeExpectancy / 1.6f).GetValue<float>();
			if (Math.Abs(num2) < 0.001f)
			{
				return 0f;
			}
			num3 = 1f;
		}
		float num6 = 1f;
		Pawn firstDirectRelationPawn = parent2?.relations.GetFirstDirectRelationPawn(PawnRelationDefOf.Spouse);
		if (firstDirectRelationPawn != null && firstDirectRelationPawn != parent2)
		{
			num6 *= 0.15f;
		}
		if (parent2 == null)
		{
			return num4 * num2 * num3 * num6;
		}
		Pawn firstDirectRelationPawn2 = parent2.relations.GetFirstDirectRelationPawn(PawnRelationDefOf.Spouse);
		if (firstDirectRelationPawn2 != null && firstDirectRelationPawn2 != parent2)
		{
			num6 *= 0.15f;
		}
		return num4 * num2 * num3 * num6;
	}

	public static bool GainTraitPrefix(Trait trait, Pawn ___pawn)
	{
		return RaceRestrictionSettings.CanGetTrait(trait.def, ___pawn, trait.Degree);
	}

	public static void TryMakeInitialRelationsWithPostfix(Faction __instance, Faction other)
	{
		ThingDef_AlienRace alienRace = GetRaceOfFaction(other.def);
		if (alienRace != null)
		{
			foreach (FactionRelationSettings frs in alienRace.alienRace.generalSettings.factionRelations)
			{
				if (frs.factions.Contains(__instance.def))
				{
					int offset = frs.goodwill.RandomInRange;
					FactionRelationKind kind = ((offset > 75) ? FactionRelationKind.Ally : ((offset > -10) ? FactionRelationKind.Neutral : FactionRelationKind.Hostile));
					FactionRelation relation = other.RelationWith(__instance);
					relation.baseGoodwill = offset;
					relation.kind = kind;
					relation = __instance.RelationWith(other);
					relation.baseGoodwill = offset;
					relation.kind = kind;
				}
			}
		}
		alienRace = GetRaceOfFaction(__instance.def);
		if (alienRace == null)
		{
			return;
		}
		foreach (FactionRelationSettings frs2 in alienRace.alienRace.generalSettings.factionRelations)
		{
			if (frs2.factions.Contains(other.def))
			{
				int offset2 = frs2.goodwill.RandomInRange;
				FactionRelationKind kind2 = ((offset2 > 75) ? FactionRelationKind.Ally : ((offset2 > -10) ? FactionRelationKind.Neutral : FactionRelationKind.Hostile));
				FactionRelation relation2 = other.RelationWith(__instance);
				relation2.baseGoodwill = offset2;
				relation2.kind = kind2;
				relation2 = __instance.RelationWith(other);
				relation2.baseGoodwill = offset2;
				relation2.kind = kind2;
			}
		}
		static ThingDef_AlienRace GetRaceOfFaction(FactionDef fac)
		{
			return (fac.basicMemberKind?.race ?? (from pgm in fac.pawnGroupMakers?.SelectMany((PawnGroupMaker pgm) => pgm.options)
				group pgm by pgm.kind.race into g
				orderby g.Count() descending
				select g).First().Key) as ThingDef_AlienRace;
		}
	}

	public static bool TryCreateThoughtPrefix(ref ThoughtDef def, SituationalThoughtHandler __instance, ref List<Thought_Situational> ___cachedThoughts)
	{
		Pawn pawn = __instance.pawn;
		if (pawn.def is ThingDef_AlienRace race)
		{
			def = race.alienRace.thoughtSettings.ReplaceIfApplicable(def);
		}
		ThoughtDef thoughtDef = def;
		for (int i = 0; i < ___cachedThoughts.Count; i++)
		{
			if (___cachedThoughts[i].def == thoughtDef)
			{
				return false;
			}
		}
		return true;
	}

	public static void CanBingeNowPostfix(Pawn pawn, ChemicalDef chemical, ref bool __result)
	{
		if (__result && pawn.def is ThingDef_AlienRace alienProps)
		{
			__result = alienProps.alienRace.generalSettings.CanUseChemical(chemical);
		}
	}

	public static IEnumerable<CodeInstruction> IngestedTranspiler(IEnumerable<CodeInstruction> instructions)
	{
		MethodInfo postIngestedInfo = AccessTools.Method(typeof(Thing), "PostIngested");
		foreach (CodeInstruction instruction in instructions)
		{
			if (instruction.Calls(postIngestedInfo))
			{
				yield return new CodeInstruction(OpCodes.Ldloc_3);
				yield return new CodeInstruction(OpCodes.Stsfld, AccessTools.Field(patchType, "ingestedCount"));
			}
			yield return instruction;
		}
	}

	public static void DrugPostIngestedPostfix(Pawn ingester, CompDrug __instance)
	{
		if (!(ingester.def is ThingDef_AlienRace alienProps))
		{
			return;
		}
		alienProps.alienRace.generalSettings.chemicalSettings?.ForEach(delegate(ChemicalSettings cs)
		{
			if (cs.chemical == __instance.Props?.chemical)
			{
				cs.reactions?.ForEach(delegate(IngestionOutcomeDoer iod)
				{
					iod.DoIngestionOutcome(ingester, __instance.parent, ingestedCount);
				});
			}
		});
	}

	public static void DrugValidatorPostfix(ref bool __result, Pawn pawn, Thing drug)
	{
		CanBingeNowPostfix(pawn, drug?.TryGetComp<CompDrug>()?.Props?.chemical, ref __result);
	}

	public static void CompatibilityWithPostfix(Pawn_RelationsTracker __instance, Pawn otherPawn, ref float __result, Pawn ___pawn)
	{
		if (___pawn.RaceProps.Humanlike != otherPawn.RaceProps.Humanlike || ___pawn == otherPawn)
		{
			__result = 0f;
			return;
		}
		float x = Mathf.Abs(___pawn.ageTracker.AgeBiologicalYearsFloat - otherPawn.ageTracker.AgeBiologicalYearsFloat);
		float num = GenMath.LerpDouble(0f, 20f, 0.45f, -0.45f, x);
		num = Mathf.Clamp(num, -0.45f, 0.45f);
		float num2 = __instance.ConstantPerPawnsPairCompatibilityOffset(otherPawn.thingIDNumber);
		__result = num + num2;
	}

	public static void SecondaryLovinChanceFactorPostfix(Pawn ___pawn, Pawn otherPawn, ref float __result)
	{
	}

	public static IEnumerable<CodeInstruction> SecondaryLovinChanceFactorTranspiler(IEnumerable<CodeInstruction> instructions)
	{
		FieldInfo defField = AccessTools.Field(typeof(Thing), "def");
		MethodInfo racePropsProperty = AccessTools.Property(typeof(Pawn), "RaceProps").GetGetMethod();
		MethodInfo humanlikeProperty = AccessTools.Property(typeof(RaceProperties), "Humanlike").GetGetMethod();
		foreach (CodeInstruction instruction in instructions)
		{
			if (instruction.opcode == OpCodes.Ldfld && instruction.OperandIs(defField))
			{
				yield return new CodeInstruction(OpCodes.Callvirt, racePropsProperty);
				instruction.opcode = OpCodes.Callvirt;
				instruction.operand = humanlikeProperty;
			}
			yield return instruction;
		}
	}

	public static void GenericHasJobOnThingPostfix(WorkGiver __instance, Pawn pawn, ref bool __result)
	{
		if (!__result)
		{
			return;
		}
		__result = (pawn.def as ThingDef_AlienRace)?.alienRace.raceRestriction.workGiverList?.Any((WorkGiverDef wgd) => wgd.giverClass == __instance.GetType()) == true || !DefDatabase<ThingDef_AlienRace>.AllDefsListForReading.Any((ThingDef_AlienRace d) => pawn.def != d && (d.alienRace.raceRestriction.workGiverList?.Any((WorkGiverDef wgd) => wgd == __instance.def) ?? false));
	}

	public static void GenericJobOnThingPostfix(WorkGiver __instance, Pawn pawn, ref Job __result)
	{
		if (__result != null && (pawn.def as ThingDef_AlienRace)?.alienRace.raceRestriction.workGiverList?.Any((WorkGiverDef wgd) => wgd.giverClass == __instance.GetType()) != true && DefDatabase<ThingDef_AlienRace>.AllDefsListForReading.Any((ThingDef_AlienRace d) => pawn.def != d && (d.alienRace.raceRestriction.workGiverList?.Any((WorkGiverDef wgd) => wgd == __instance.def) ?? false)))
		{
			__result = null;
		}
	}

	public static void SetFactionDirectPostfix(Thing __instance, Faction newFaction)
	{
		if (!(__instance.def is ThingDef_AlienRace alienProps) || newFaction != Faction.OfPlayerSilentFail)
		{
			return;
		}
		alienProps.alienRace.raceRestriction.conceptList?.ForEach(delegate(ConceptDef cdd)
		{
			if (cdd != null)
			{
				Find.Tutor.learningReadout.TryActivateConcept(cdd);
				PlayerKnowledgeDatabase.SetKnowledge(cdd, 0f);
			}
		});
	}

	public static void SetFactionPostfix(Pawn __instance, Faction newFaction)
	{
		if (!(__instance.def is ThingDef_AlienRace alienProps) || newFaction != Faction.OfPlayerSilentFail || Current.ProgramState != ProgramState.Playing)
		{
			return;
		}
		alienProps.alienRace.raceRestriction.conceptList?.ForEach(delegate(ConceptDef cdd)
		{
			if (cdd != null)
			{
				Find.Tutor.learningReadout.TryActivateConcept(cdd);
				PlayerKnowledgeDatabase.SetKnowledge(cdd, 0f);
			}
		});
	}

	public static void ApparelScoreGainPostFix(Pawn pawn, Apparel ap, ref float __result)
	{
		if (__result >= 0f && !RaceRestrictionSettings.CanWear(ap.def, pawn.def))
		{
			__result = -1001f;
		}
	}

	public static void PrepForMapGenPrefix(GameInitData __instance)
	{
		foreach (ScenPart part in Find.Scenario.AllParts)
		{
			if (part is ScenPart_StartingHumanlikes sp1)
			{
				IEnumerable<Pawn> sp2 = sp1.GetPawns();
				Pawn[] spa = (sp2 as Pawn[]) ?? sp2.ToArray();
				__instance.startingAndOptionalPawns.InsertRange(__instance.startingPawnCount, spa);
				__instance.startingPawnCount += spa.Length;
				Pawn[] array = spa;
				foreach (Pawn pawn in array)
				{
					CachedData.generateStartingPossessions(pawn);
				}
			}
		}
	}

	public static bool TryGainMemoryPrefix(ref Thought_Memory newThought, MemoryThoughtHandler __instance)
	{
		Pawn pawn = __instance.pawn;
		if (!(pawn.def is ThingDef_AlienRace race))
		{
			return true;
		}
		ThoughtDef newThoughtDef = race.alienRace.thoughtSettings.ReplaceIfApplicable(newThought.def);
		if (newThoughtDef == newThought.def)
		{
			return true;
		}
		Thought_Memory replacedThought = ThoughtMaker.MakeThought(newThoughtDef, newThought.CurStageIndex);
		newThought = replacedThought;
		return true;
	}

	public static void ExtraRequirementsGrowerSowPostfix(Pawn pawn, IPlantToGrowSettable settable, ref bool __result)
	{
		if (__result)
		{
			ThingDef plant = WorkGiver_Grower.CalculateWantedPlantDef((settable as Zone_Growing)?.Cells[0] ?? ((Thing)settable).Position, pawn.Map);
			__result = RaceRestrictionSettings.CanPlant(plant, pawn.def);
		}
	}

	public static void HasJobOnCellHarvestPostfix(Pawn pawn, IntVec3 c, ref bool __result)
	{
		if (__result)
		{
			ThingDef plant = c.GetPlant(pawn.Map).def;
			__result = RaceRestrictionSettings.CanPlant(plant, pawn.def);
		}
	}

	public static void PawnAllowedToStartAnewPostfix(Pawn p, Bill __instance, ref bool __result)
	{
		RecipeDef recipe = __instance.recipe;
		if (__result)
		{
			__result = RaceRestrictionSettings.CanDoRecipe(recipe, p.def);
		}
	}

	public static void UpdateColonistRaces()
	{
		if (Find.TickManager.TicksAbs <= colonistRacesTick + 5000 && Find.TickManager.TicksAbs >= colonistRacesTick)
		{
			return;
		}
		List<Pawn> pawns = PawnsFinder.AllMaps_FreeColonistsSpawned;
		if (pawns.Count > 0)
		{
			HashSet<ThingDef> hashSet = new HashSet<ThingDef>();
			foreach (ThingDef item in pawns.Select((Pawn p) => p.def))
			{
				hashSet.Add(item);
			}
			HashSet<ThingDef> newColonistRaces = hashSet;
			colonistRacesTick = Find.TickManager.TicksAbs;
			if (newColonistRaces.Count == colonistRaces.Count && !newColonistRaces.Any((ThingDef item) => !colonistRaces.Contains(item)))
			{
				return;
			}
			RaceRestrictionSettings.buildingsRestrictedWithCurrentColony.Clear();
			HashSet<BuildableDef> hashSet2 = new HashSet<BuildableDef>();
			foreach (BuildableDef item2 in RaceRestrictionSettings.buildingRestricted)
			{
				hashSet2.Add(item2);
			}
			HashSet<BuildableDef> buildingsRestrictedTemp = hashSet2;
			buildingsRestrictedTemp.AddRange(newColonistRaces.Where((ThingDef thingDef) => thingDef is ThingDef_AlienRace).SelectMany((ThingDef thingDef) => (thingDef as ThingDef_AlienRace).alienRace.raceRestriction.blackBuildingList));
			foreach (BuildableDef td in buildingsRestrictedTemp)
			{
				bool canBuild = false;
				foreach (ThingDef race in newColonistRaces)
				{
					if (RaceRestrictionSettings.CanBuild(td, race))
					{
						canBuild = true;
					}
				}
				if (!canBuild)
				{
					RaceRestrictionSettings.buildingsRestrictedWithCurrentColony.Add(td);
				}
			}
			foreach (BuildableDef td2 in RaceRestrictionSettings.buildingsHidden)
			{
				bool hidden = true;
				foreach (ThingDef race2 in newColonistRaces)
				{
					if (!(race2 is ThingDef_AlienRace alienRace) || !alienRace.alienRace.raceRestriction.hiddenBuildingList.Contains(td2))
					{
						hidden = false;
					}
				}
				if (hidden)
				{
					RaceRestrictionSettings.buildingsRestrictedWithCurrentColony.Add(td2);
				}
			}
			colonistRaces = newColonistRaces;
		}
		else
		{
			colonistRaces.Clear();
			colonistRacesTick = Find.TickManager.TicksAbs - 5000 + 60;
			RaceRestrictionSettings.buildingsRestrictedWithCurrentColony.Clear();
			RaceRestrictionSettings.buildingsRestrictedWithCurrentColony.AddRange(RaceRestrictionSettings.buildingRestricted);
		}
	}

	public static IEnumerable<CodeInstruction> DesignatorAllowedTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
	{
		Label gotoReturn = ilg.DefineLabel();
		foreach (CodeInstruction instruction in instructions)
		{
			if (instruction.opcode == OpCodes.Ret)
			{
				yield return new CodeInstruction(OpCodes.Dup).MoveLabelsFrom(instruction);
				yield return new CodeInstruction(OpCodes.Brfalse, gotoReturn);
				yield return new CodeInstruction(OpCodes.Ldarg_1);
				yield return new CodeInstruction(OpCodes.Isinst, typeof(Designator_Build));
				yield return new CodeInstruction(OpCodes.Brfalse, gotoReturn);
				yield return new CodeInstruction(OpCodes.Pop);
				yield return new CodeInstruction(OpCodes.Ldarg_1);
				yield return new CodeInstruction(OpCodes.Castclass, typeof(Designator_Build));
				yield return CodeInstruction.Call(patchType, "DesignatorAllowedHelper");
				instruction.labels.Add(gotoReturn);
			}
			yield return instruction;
		}
	}

	public static bool DesignatorAllowedHelper(Designator_Build d)
	{
		UpdateColonistRaces();
		return RaceRestrictionSettings.CanColonyBuild(d.PlacingDef);
	}

	public static void CanConstructPostfix(Thing t, Pawn p, ref bool __result)
	{
		if (__result)
		{
			__result = RaceRestrictionSettings.CanBuild(t.def.entityDefToBuild ?? t.def, p.def);
		}
	}

	public static IEnumerable<CodeInstruction> ResearchScreenTranspiler(IEnumerable<CodeInstruction> instructions)
	{
		MethodInfo defListInfo = AccessTools.PropertyGetter(typeof(DefDatabase<ResearchProjectDef>), "AllDefsListForReading");
		foreach (CodeInstruction instruction in instructions)
		{
			if (instruction.opcode == OpCodes.Call && instruction.OperandIs(defListInfo))
			{
				yield return instruction;
				yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(patchType, "ResearchFixed"));
			}
			else
			{
				yield return instruction;
			}
		}
	}

	private static List<ResearchProjectDef> ResearchFixed(List<ResearchProjectDef> researchList)
	{
		UpdateColonistRaces();
		return researchList.Where((ResearchProjectDef rpd) => RaceRestrictionSettings.CanResearch(colonistRaces, rpd)).ToList();
	}

	public static void ShouldSkipResearchPostfix(Pawn pawn, ref bool __result)
	{
		if (__result)
		{
			return;
		}
		ResearchProjectDef project = Find.ResearchManager.GetProject();
		ResearchProjectRestrictions rprest = (pawn.def as ThingDef_AlienRace)?.alienRace.raceRestriction.researchList?.FirstOrDefault((ResearchProjectRestrictions rpr) => rpr.projects.Contains(project));
		if (rprest != null)
		{
			IEnumerable<ThingDef> apparel = pawn.apparel.WornApparel.Select((Apparel twc) => twc.def);
			List<ThingDef> list = rprest.apparelList;
			if (list != null && !list.TrueForAll((ThingDef ap) => apparel.Contains(ap)))
			{
				__result = true;
			}
			return;
		}
		__result = DefDatabase<ThingDef_AlienRace>.AllDefsListForReading.Any((ThingDef_AlienRace d) => pawn.def != d && (d.alienRace.raceRestriction.researchList?.Any((ResearchProjectRestrictions rpr) => rpr.projects.Contains(project)) ?? false));
	}

	public static void ThoughtsFromIngestingPostfix(Pawn ingester, Thing foodSource, ThingDef foodDef, ref List<FoodUtility.ThoughtFromIngesting> __result)
	{
		try
		{
			if (!(ingester.def is ThingDef_AlienRace alienProps))
			{
				return;
			}
			if (ingester.story.traits.HasTrait(AlienDefOf.HAR_Xenophobia) && ingester.story.traits.DegreeOfTrait(AlienDefOf.HAR_Xenophobia) == 1)
			{
				if (__result.Any((FoodUtility.ThoughtFromIngesting tfi) => tfi.thought == AlienDefOf.AteHumanlikeMeatDirect) && foodDef.ingestible?.sourceDef != ingester.def)
				{
					__result.RemoveAll((FoodUtility.ThoughtFromIngesting tfi) => tfi.thought == AlienDefOf.AteHumanlikeMeatDirect);
				}
				else if (__result.Any((FoodUtility.ThoughtFromIngesting tfi) => tfi.thought == AlienDefOf.AteHumanlikeMeatAsIngredient) && foodSource?.TryGetComp<CompIngredients>()?.ingredients?.Any((ThingDef td) => FoodUtility.GetMeatSourceCategory(td) == MeatSourceCategory.Humanlike && td.ingestible?.sourceDef != ingester.def) == true)
				{
					__result.RemoveAll((FoodUtility.ThoughtFromIngesting tfi) => tfi.thought == AlienDefOf.AteHumanlikeMeatAsIngredient);
				}
			}
			bool cannibal = ingester.story.traits.HasTrait(AlienDefOf.Cannibal);
			List<FoodUtility.ThoughtFromIngesting> resultingThoughts = new List<FoodUtility.ThoughtFromIngesting>();
			for (int i = 0; i < __result.Count; i++)
			{
				ThoughtDef thoughtDef = __result[i].thought;
				ThoughtSettings settings = alienProps.alienRace.thoughtSettings;
				thoughtDef = settings.ReplaceIfApplicable(thoughtDef);
				if (thoughtDef == AlienDefOf.AteHumanlikeMeatDirect || thoughtDef == AlienDefOf.AteHumanlikeMeatDirectCannibal)
				{
					thoughtDef = settings.GetAteThought(foodDef.ingestible?.sourceDef, cannibal, ingredient: false);
				}
				if (thoughtDef == AlienDefOf.AteHumanlikeMeatAsIngredient || thoughtDef == AlienDefOf.AteHumanlikeMeatAsIngredientCannibal)
				{
					ThingDef race = foodSource?.TryGetComp<CompIngredients>()?.ingredients?.FirstOrDefault((ThingDef td) => td.ingestible?.sourceDef?.race?.Humanlike == true)?.ingestible?.sourceDef;
					if (race != null)
					{
						thoughtDef = settings.GetAteThought(race, cannibal, ingredient: true);
					}
				}
				resultingThoughts.Add(new FoodUtility.ThoughtFromIngesting
				{
					fromPrecept = __result[i].fromPrecept,
					thought = thoughtDef
				});
			}
			__result = resultingThoughts;
			if (foodSource != null && FoodUtility.IsHumanlikeCorpseOrHumanlikeMeatOrIngredient(foodSource))
			{
				bool alienMeat = false;
				CompIngredients compIngredients = foodSource.TryGetComp<CompIngredients>();
				if (compIngredients?.ingredients != null)
				{
					foreach (ThingDef ingredient in compIngredients.ingredients)
					{
						if (ingredient.IsMeat && ingredient.ingestible.sourceDef != ingester.def)
						{
							alienMeat = true;
						}
					}
				}
				CachedData.ingestThoughts().Clear();
				CachedData.foodUtilityAddThoughtsFromIdeo(alienMeat ? AlienDefOf.HAR_AteAlienMeat : AlienDefOf.HAR_AteNonAlienFood, ingester, foodDef, alienMeat ? MeatSourceCategory.Humanlike : MeatSourceCategory.NotMeat);
				resultingThoughts.AddRange(CachedData.ingestThoughts());
			}
			__result = resultingThoughts;
		}
		catch (Exception ex)
		{
			Log.Error($"AlienRace encountered an error processing food\nPawn: {ingester?.Name} | {ingester?.def.defName}\nFood: {foodDef?.defName} | {foodSource?.def.defName} | {foodDef?.modContentPack?.Name}\n{ex}");
		}
	}

	public static void GenerationChanceSpousePostfix(ref float __result, Pawn generated, Pawn other)
	{
		if (generated.def is ThingDef_AlienRace race)
		{
			__result *= race.alienRace.relationSettings.relationChanceModifierSpouse;
		}
		if (other.def is ThingDef_AlienRace alienRace)
		{
			__result *= alienRace.alienRace.relationSettings.relationChanceModifierSpouse;
		}
		__result *= (generated.story.GetBackstory(BackstorySlot.Childhood) as AlienBackstoryDef)?.relationSettings.relationChanceModifierSpouse ?? 1f;
		__result *= (generated.story.GetBackstory(BackstorySlot.Adulthood) as AlienBackstoryDef)?.relationSettings.relationChanceModifierSpouse ?? 1f;
		__result *= (other.story.GetBackstory(BackstorySlot.Childhood) as AlienBackstoryDef)?.relationSettings.relationChanceModifierSpouse ?? 1f;
		__result *= (other.story.GetBackstory(BackstorySlot.Adulthood) as AlienBackstoryDef)?.relationSettings.relationChanceModifierSpouse ?? 1f;
		if (generated == other)
		{
			__result = 0f;
		}
	}

	public static void GenerationChanceSiblingPostfix(ref float __result, Pawn generated, Pawn other)
	{
		if (generated.def is ThingDef_AlienRace race)
		{
			__result *= race.alienRace.relationSettings.relationChanceModifierSibling;
		}
		if (other.def is ThingDef_AlienRace alienRace)
		{
			__result *= alienRace.alienRace.relationSettings.relationChanceModifierSibling;
		}
		__result *= (generated.story.GetBackstory(BackstorySlot.Childhood) as AlienBackstoryDef)?.relationSettings.relationChanceModifierSibling ?? 1f;
		__result *= (generated.story.GetBackstory(BackstorySlot.Adulthood) as AlienBackstoryDef)?.relationSettings.relationChanceModifierSibling ?? 1f;
		__result *= (other.story.GetBackstory(BackstorySlot.Childhood) as AlienBackstoryDef)?.relationSettings.relationChanceModifierSibling ?? 1f;
		__result *= (other.story.GetBackstory(BackstorySlot.Adulthood) as AlienBackstoryDef)?.relationSettings.relationChanceModifierSibling ?? 1f;
		if (generated == other)
		{
			__result = 0f;
		}
	}

	public static void GenerationChanceParentPostfix(ref float __result, Pawn generated, Pawn other)
	{
		if (generated.def is ThingDef_AlienRace race)
		{
			__result *= race.alienRace.relationSettings.relationChanceModifierParent;
		}
		if (other.def is ThingDef_AlienRace alienRace)
		{
			__result *= alienRace.alienRace.relationSettings.relationChanceModifierParent;
		}
		__result *= (generated.story.GetBackstory(BackstorySlot.Childhood) as AlienBackstoryDef)?.relationSettings.relationChanceModifierParent ?? 1f;
		__result *= (generated.story.GetBackstory(BackstorySlot.Adulthood) as AlienBackstoryDef)?.relationSettings.relationChanceModifierParent ?? 1f;
		__result *= (other.story.GetBackstory(BackstorySlot.Childhood) as AlienBackstoryDef)?.relationSettings.relationChanceModifierParent ?? 1f;
		__result *= (other.story.GetBackstory(BackstorySlot.Adulthood) as AlienBackstoryDef)?.relationSettings.relationChanceModifierParent ?? 1f;
		if (generated == other)
		{
			__result = 0f;
		}
	}

	public static void GenerationChanceLoverPostfix(ref float __result, Pawn generated, Pawn other)
	{
		if (generated.def is ThingDef_AlienRace race)
		{
			__result *= race.alienRace.relationSettings.relationChanceModifierLover;
		}
		if (other.def is ThingDef_AlienRace alienRace)
		{
			__result *= alienRace.alienRace.relationSettings.relationChanceModifierLover;
		}
		__result *= (generated.story.GetBackstory(BackstorySlot.Childhood) as AlienBackstoryDef)?.relationSettings.relationChanceModifierLover ?? 1f;
		__result *= (generated.story.GetBackstory(BackstorySlot.Adulthood) as AlienBackstoryDef)?.relationSettings.relationChanceModifierLover ?? 1f;
		__result *= (other.story.GetBackstory(BackstorySlot.Childhood) as AlienBackstoryDef)?.relationSettings.relationChanceModifierLover ?? 1f;
		__result *= (other.story.GetBackstory(BackstorySlot.Adulthood) as AlienBackstoryDef)?.relationSettings.relationChanceModifierLover ?? 1f;
		if (generated == other)
		{
			__result = 0f;
		}
	}

	public static void GenerationChanceFiancePostfix(ref float __result, Pawn generated, Pawn other)
	{
		if (generated.def is ThingDef_AlienRace race)
		{
			__result *= race.alienRace.relationSettings.relationChanceModifierFiance;
		}
		if (other.def is ThingDef_AlienRace alienRace)
		{
			__result *= alienRace.alienRace.relationSettings.relationChanceModifierFiance;
		}
		__result *= (generated.story.GetBackstory(BackstorySlot.Childhood) as AlienBackstoryDef)?.relationSettings.relationChanceModifierFiance ?? 1f;
		__result *= (generated.story.GetBackstory(BackstorySlot.Adulthood) as AlienBackstoryDef)?.relationSettings.relationChanceModifierFiance ?? 1f;
		__result *= (other.story.GetBackstory(BackstorySlot.Childhood) as AlienBackstoryDef)?.relationSettings.relationChanceModifierFiance ?? 1f;
		__result *= (other.story.GetBackstory(BackstorySlot.Adulthood) as AlienBackstoryDef)?.relationSettings.relationChanceModifierFiance ?? 1f;
		if (generated == other)
		{
			__result = 0f;
		}
	}

	public static void GenerationChanceExSpousePostfix(ref float __result, Pawn generated, Pawn other)
	{
		if (generated.def is ThingDef_AlienRace race)
		{
			__result *= race.alienRace.relationSettings.relationChanceModifierExSpouse;
		}
		if (other.def is ThingDef_AlienRace alienRace)
		{
			__result *= alienRace.alienRace.relationSettings.relationChanceModifierExSpouse;
		}
		__result *= (generated.story.GetBackstory(BackstorySlot.Childhood) as AlienBackstoryDef)?.relationSettings.relationChanceModifierExSpouse ?? 1f;
		__result *= (generated.story.GetBackstory(BackstorySlot.Adulthood) as AlienBackstoryDef)?.relationSettings.relationChanceModifierExSpouse ?? 1f;
		__result *= (other.story.GetBackstory(BackstorySlot.Childhood) as AlienBackstoryDef)?.relationSettings.relationChanceModifierExSpouse ?? 1f;
		__result *= (other.story.GetBackstory(BackstorySlot.Adulthood) as AlienBackstoryDef)?.relationSettings.relationChanceModifierExSpouse ?? 1f;
		if (generated == other)
		{
			__result = 0f;
		}
	}

	public static void GenerationChanceExLoverPostfix(ref float __result, Pawn generated, Pawn other)
	{
		if (generated.def is ThingDef_AlienRace race)
		{
			__result *= race.alienRace.relationSettings.relationChanceModifierExLover;
		}
		if (other.def is ThingDef_AlienRace alienRace)
		{
			__result *= alienRace.alienRace.relationSettings.relationChanceModifierExLover;
		}
		__result *= (generated.story.GetBackstory(BackstorySlot.Childhood) as AlienBackstoryDef)?.relationSettings.relationChanceModifierExLover ?? 1f;
		__result *= (generated.story.GetBackstory(BackstorySlot.Adulthood) as AlienBackstoryDef)?.relationSettings.relationChanceModifierExLover ?? 1f;
		__result *= (other.story.GetBackstory(BackstorySlot.Childhood) as AlienBackstoryDef)?.relationSettings.relationChanceModifierExLover ?? 1f;
		__result *= (other.story.GetBackstory(BackstorySlot.Adulthood) as AlienBackstoryDef)?.relationSettings.relationChanceModifierExLover ?? 1f;
		if (generated == other)
		{
			__result = 0f;
		}
	}

	public static void GenerationChanceChildPostfix(ref float __result, Pawn generated, Pawn other)
	{
		if (generated.def is ThingDef_AlienRace race)
		{
			__result *= race.alienRace.relationSettings.relationChanceModifierChild;
		}
		if (other.def is ThingDef_AlienRace alienRace)
		{
			__result *= alienRace.alienRace.relationSettings.relationChanceModifierChild;
		}
		__result *= (generated.story.GetBackstory(BackstorySlot.Childhood) as AlienBackstoryDef)?.relationSettings.relationChanceModifierChild ?? 1f;
		__result *= (generated.story.GetBackstory(BackstorySlot.Adulthood) as AlienBackstoryDef)?.relationSettings.relationChanceModifierChild ?? 1f;
		__result *= (other.story.GetBackstory(BackstorySlot.Childhood) as AlienBackstoryDef)?.relationSettings.relationChanceModifierChild ?? 1f;
		__result *= (other.story.GetBackstory(BackstorySlot.Adulthood) as AlienBackstoryDef)?.relationSettings.relationChanceModifierChild ?? 1f;
		if (generated == other)
		{
			__result = 0f;
		}
	}

	public static void BirthdayBiologicalPrefix(Pawn ___pawn)
	{
		if (___pawn.def is ThingDef_AlienRace && ___pawn.def.race.lifeStageAges.Skip(1).Any() && ___pawn.ageTracker.CurLifeStageIndex != 0)
		{
			LifeStageAge lsac = ___pawn.ageTracker.CurLifeStageRace;
			LifeStageAge lsap = ___pawn.def.race.lifeStageAges[___pawn.ageTracker.CurLifeStageIndex - 1];
			if ((lsac is LifeStageAgeAlien { body: not null } lsaac && ((lsap as LifeStageAgeAlien)?.body ?? ___pawn.RaceProps.body) != lsaac.body) || (lsap is LifeStageAgeAlien { body: not null } lsaap && ((lsac as LifeStageAgeAlien)?.body ?? ___pawn.RaceProps.body) != lsaap.body))
			{
				___pawn.health.hediffSet = new HediffSet(___pawn);
			}
		}
	}

	public static void CanEquipPostfix(ref bool __result, Thing thing, Pawn pawn, ref string cantReason)
	{
		if (__result)
		{
			if (thing.def.IsApparel && !RaceRestrictionSettings.CanWear(thing.def, pawn.def))
			{
				__result = false;
				cantReason = $"{pawn.def.LabelCap} can't wear this";
			}
			else if (thing.def.IsWeapon && !RaceRestrictionSettings.CanEquip(thing.def, pawn.def))
			{
				__result = false;
				cantReason = $"{pawn.def.LabelCap} can't equip this";
			}
		}
	}

	public static void CanGetThoughtPostfix(ref bool __result, ThoughtDef def, Pawn pawn)
	{
		if (__result)
		{
			__result = ThoughtSettings.CanGetThought(def, pawn);
		}
	}

	public static IEnumerable<CodeInstruction> CanDoNextStartPawnTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
	{
		MethodInfo getNameInfo = AccessTools.PropertyGetter(typeof(Pawn), "Name");
		LocalBuilder pawnLocal = ilg.DeclareLocal(typeof(Pawn));
		List<CodeInstruction> instructionList = instructions.ToList();
		for (int index = 0; index < instructionList.Count; index++)
		{
			CodeInstruction instruction = instructionList[index];
			if (instruction.Calls(getNameInfo))
			{
				yield return new CodeInstruction(OpCodes.Dup);
				yield return new CodeInstruction(OpCodes.Stloc, pawnLocal.LocalIndex);
				yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Pawn), "def"));
				yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ThingDef), "race"));
				yield return new CodeInstruction(OpCodes.Ldloc, pawnLocal.LocalIndex);
				yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Pawn), "gender"));
				yield return CodeInstruction.Call(typeof(RaceProperties), "GetNameGenerator");
				yield return new CodeInstruction(instructionList[index + 2]);
				yield return new CodeInstruction(OpCodes.Ldloc, pawnLocal.LocalIndex);
			}
			yield return instruction;
		}
	}

	public static IEnumerable<CodeInstruction> DrawCharacterCardTranspiler(IEnumerable<CodeInstruction> instructions)
	{
		MethodInfo originalMethod = AccessTools.Method(typeof(Widgets), "ButtonText", new Type[6]
		{
			typeof(Rect),
			typeof(string),
			typeof(bool),
			typeof(bool),
			typeof(bool),
			typeof(TextAnchor?)
		});
		MethodInfo myPassthroughMethod = AccessTools.Method(patchType, "PawnKindRandomizeButtonPassthrough");
		bool found = false;
		foreach (CodeInstruction instruction in instructions)
		{
			if (instruction.Calls(originalMethod))
			{
				found = true;
				yield return new CodeInstruction(OpCodes.Call, myPassthroughMethod);
			}
			else
			{
				yield return instruction;
			}
		}
		if (!found)
		{
			Log.Error("(Humanoid Alien Races) Unable to find injection point for character card randomization transpiler, the target may have been changed or transpiled by another mod.");
		}
	}

	private static bool PawnKindRandomizeButtonPassthrough(Rect rect, string label, bool drawBackground = true, bool doMouseoverSound = true, bool active = true, TextAnchor? overrideTextAnchor = null)
	{
		Rect rightRect = rect;
		rightRect.x += 154f;
		rightRect.width = 46f;
		if (Mouse.IsOver(rightRect) && Find.WindowStack.FloatMenu == null)
		{
			TaggedString tipString = "HAR.StartingRace".Translate(startingPawnKindLabel ?? ((string)"None".Translate())).Colorize(ColoredText.TipSectionTitleColor) + "\n\n" + "HAR.StartingRaceDescription".Translate();
			TooltipHandler.TipRegion(rightRect, tipString.Resolve());
		}
		if (Widgets.ButtonImageWithBG(rightRect, (startingPawnKindRestriction == null) ? CachedData.Textures.AlienIconInactive : CachedData.Textures.AlienIconActive, new Vector2(22f, 22f)))
		{
			DoStartingPawnKindDropdown();
		}
		rect.width = 150f;
		return Widgets.ButtonText(rect, label, drawBackground, doMouseoverSound, active, overrideTextAnchor);
	}

	private static void DoStartingPawnKindDropdown()
	{
		List<PawnKindDef> startingPawnKinds = new List<PawnKindDef>();
		HashSet<string> startingPawnKindLabelSet = new HashSet<string>();
		HashSet<string> startingPawnKindDuplicateLabelSet = new HashSet<string>();
		PawnKindDef basicMemberKind = Find.GameInitData.startingPawnKind ?? Faction.OfPlayer.def.basicMemberKind;
		List<FloatMenuOption> options = new List<FloatMenuOption>(1)
		{
			new FloatMenuOption("NoneBrackets".Translate(), delegate
			{
				startingPawnKindRestriction = null;
				startingPawnKindLabel = "None".Translate();
			})
		};
		foreach (PawnKindEntry entry in NewGeneratedStartingPawnKinds(basicMemberKind))
		{
			foreach (PawnKindDef kind in entry.kindDefs)
			{
				if (!startingPawnKinds.Contains(kind))
				{
					startingPawnKinds.Add(kind);
					if (!startingPawnKindLabelSet.Add(kind.label))
					{
						startingPawnKindDuplicateLabelSet.Add(kind.label);
					}
				}
			}
		}
		foreach (PawnKindDef kind2 in startingPawnKinds)
		{
			string label;
			if (startingPawnKindDuplicateLabelSet.Contains(kind2.label))
			{
				label = $"{kind2.race.LabelCap} ({kind2.defName})";
			}
			else if (kind2.label == kind2.race.label)
			{
				label = kind2.race.LabelCap;
			}
			else
			{
				label = $"{kind2.race.LabelCap} ({kind2.label})";
			}
			options.Add(new FloatMenuOption(label, delegate
			{
				startingPawnKindRestriction = kind2;
				startingPawnKindLabel = label;
			}));
		}
		Find.WindowStack.Add(new FloatMenu(options));
	}

	public static bool GeneratePawnNamePrefix(ref Name __result, Pawn pawn, NameStyle style = NameStyle.Full, string forcedLastName = null)
	{
		if (!(pawn.def is ThingDef_AlienRace alienProps) || alienProps.race.GetNameGenerator(pawn.gender) == null || style != NameStyle.Full || pawn.kindDef.GetNameMaker(pawn.gender) != null)
		{
			return true;
		}
		NameTriple nameTriple = NameTriple.FromString(NameGenerator.GenerateName(alienProps.race.GetNameGenerator(pawn.gender)));
		string first = nameTriple.First;
		string nick = nameTriple.Nick;
		string last = nameTriple.Last;
		if (nick == null)
		{
			nick = nameTriple.First;
		}
		if (last != null && forcedLastName != null)
		{
			last = forcedLastName;
		}
		__result = new NameTriple(first ?? string.Empty, nick ?? string.Empty, last ?? string.Empty);
		return false;
	}

	public static void GenerateRandomAgePrefix(Pawn pawn, PawnGenerationRequest request)
	{
		AlienPartGenerator.AlienComp alienComp = pawn.TryGetComp<AlienPartGenerator.AlienComp>();
		if (!request.FixedGender.HasValue && !pawn.kindDef.fixedGender.HasValue && pawn.RaceProps.hasGenders)
		{
			Info modExtension = pawn.kindDef.GetModExtension<Info>();
			float? maleGenderProbability = ((modExtension != null) ? new float?(modExtension.maleGenderProbability) : (pawn.def as ThingDef_AlienRace)?.alienRace.generalSettings.maleGenderProbability);
			if (!maleGenderProbability.HasValue)
			{
				return;
			}
			pawn.gender = ((!(Rand.Value >= maleGenderProbability)) ? Gender.Male : Gender.Female);
			if ((alienComp == null || !(Math.Abs(maleGenderProbability.Value) < 0.001f)) && !(Math.Abs(maleGenderProbability.Value - 1f) < 0.001f))
			{
				return;
			}
			if (alienComp != null)
			{
				alienComp.fixGenderPostSpawn = true;
			}
		}
		if (alienComp != null && pawn.kindDef.forcedHairColor.HasValue)
		{
			alienComp.OverwriteColorChannel("hair", pawn.kindDef.forcedHairColor.Value);
		}
		if (alienComp != null && pawn.kindDef.skinColorOverride.HasValue)
		{
			alienComp.OverwriteColorChannel("skin", pawn.kindDef.skinColorOverride.Value);
		}
	}

	public static IEnumerable<CodeInstruction> GenerateGenesTranspiler(IEnumerable<CodeInstruction> instructions)
	{
		List<CodeInstruction> instructionList = instructions.ToList();
		MethodInfo randomGreyColorInfo = AccessTools.Method(typeof(PawnHairColors), "RandomGreyHairColor");
		bool foundForcedGeneEntry = false;
		for (int i = 0; i < instructionList.Count; i++)
		{
			CodeInstruction instruction = instructionList[i];
			if (!foundForcedGeneEntry && instruction.opcode == OpCodes.Ldarg_1)
			{
				foundForcedGeneEntry = true;
				yield return new CodeInstruction(OpCodes.Ldarg_0);
				yield return CodeInstruction.Call(patchType, "GenerateGenesForcedHelper");
			}
			yield return instruction;
			if (instructionList[i].Calls(randomGreyColorInfo))
			{
				yield return new CodeInstruction(OpCodes.Ldarg_0);
				yield return CodeInstruction.Call(patchType, "OldHairColorHelper");
			}
		}
	}

	public static Color OldHairColorHelper(Color originalColor, Pawn pawn)
	{
		if (!(pawn.def is ThingDef_AlienRace alienProps))
		{
			return originalColor;
		}
		return pawn.GetComp<AlienPartGenerator.AlienComp>().GenerateColor(alienProps.alienRace.generalSettings.alienPartGenerator.oldHairColorGen);
	}

	public static void GenerateGenesForcedHelper(Pawn pawn)
	{
		if (!(pawn.def is ThingDef_AlienRace alienProps))
		{
			return;
		}
		foreach (AlienChanceEntry<GeneDef> gene in alienProps.alienRace.generalSettings.raceGenes)
		{
			foreach (GeneDef option in gene.Select(pawn))
			{
				pawn.genes.AddGene(option, xenogene: false);
			}
		}
	}

	public static void GenerateGenesPrefix(Pawn pawn, ref PawnGenerationRequest request)
	{
		if (pawn.def is ThingDef_AlienRace)
		{
			if (pawn.story.HairColor == Color.white)
			{
				pawn.story.HairColor = Color.clear;
			}
			AlienPartGenerator.AlienComp alienComp = pawn.GetComp<AlienPartGenerator.AlienComp>();
			pawn.story.SkinColorBase = alienComp.GetChannel("skin").first;
		}
	}

	public static void GenerateGenesPostfix(Pawn pawn)
	{
		if (pawn.def is ThingDef_AlienRace)
		{
			AlienPartGenerator.AlienComp alienComp = pawn.GetComp<AlienPartGenerator.AlienComp>();
			if (pawn.story.HairColor == Color.clear)
			{
				pawn.story.HairColor = alienComp.GetChannel("hair").first;
			}
			else if (alienComp.GetChannel("hair").first == Color.clear)
			{
				alienComp.OverwriteColorChannel("hair", pawn.story.HairColor);
			}
		}
	}

	public static IEnumerable<CodeInstruction> DefaultStartingPawnTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
	{
		FieldInfo basicMemberInfo = AccessTools.Field(typeof(FactionDef), "basicMemberKind");
		FieldInfo baseLinerInfo = AccessTools.Field(typeof(XenotypeDefOf), "Baseliner");
		LocalBuilder xenotypeDefLocal = ilg.DeclareLocal(typeof(XenotypeDef));
		LocalBuilder xenotypeCustomLocal = ilg.DeclareLocal(typeof(CustomXenotype));
		LocalBuilder developmentalStageLocal = ilg.DeclareLocal(typeof(DevelopmentalStage));
		LocalBuilder allowDownedLocal = ilg.DeclareLocal(typeof(bool));
		bool downedDone = false;
		List<CodeInstruction> instructionList = instructions.ToList();
		for (int i = 0; i < instructionList.Count; i++)
		{
			CodeInstruction instruction = instructionList[i];
			yield return instruction;
			if (instruction.LoadsField(basicMemberInfo))
			{
				yield return CodeInstruction.Call(patchType, "NewGeneratedStartingPawnHelper").WithLabels(instructionList[i + 1].ExtractLabels());
				yield return new CodeInstruction(OpCodes.Dup)
				{
					labels = instructionList[i + 1].ExtractLabels()
				};
				yield return new CodeInstruction(OpCodes.Ldloca, xenotypeDefLocal.LocalIndex);
				yield return new CodeInstruction(OpCodes.Ldloca, xenotypeCustomLocal.LocalIndex);
				yield return new CodeInstruction(OpCodes.Ldloca, developmentalStageLocal.LocalIndex);
				yield return new CodeInstruction(OpCodes.Ldloca, allowDownedLocal.LocalIndex);
				yield return CodeInstruction.Call(patchType, "PickStartingPawnConfig");
			}
			else if (!downedDone && instruction.opcode == OpCodes.Ldc_I4_0 && instructionList[i - 1].opcode == OpCodes.Ldc_I4_0 && instructionList[i + 1].opcode == OpCodes.Ldc_I4_1)
			{
				downedDone = true;
				yield return new CodeInstruction(OpCodes.Pop);
				yield return new CodeInstruction(OpCodes.Ldloc, allowDownedLocal.LocalIndex);
			}
			else if (instruction.LoadsField(baseLinerInfo))
			{
				yield return new CodeInstruction(OpCodes.Pop);
				yield return new CodeInstruction(OpCodes.Ldloc, xenotypeDefLocal.LocalIndex);
			}
			else if (instruction.opcode == OpCodes.Ldloc_1)
			{
				yield return new CodeInstruction(OpCodes.Ldloc, xenotypeCustomLocal.LocalIndex).WithLabels(instructionList[i + 1].labels);
				yield return instructionList[i + 2];
				yield return instructionList[i + 3];
				yield return new CodeInstruction(OpCodes.Ldloc, developmentalStageLocal.LocalIndex);
				i += 4;
			}
		}
	}

	public static void PickStartingPawnConfig(PawnKindDef kindDef, out XenotypeDef xenotypeDef, out CustomXenotype xenotypeCustom, out DevelopmentalStage devStage, out bool allowDowned)
	{
		xenotypeDef = currentStartingRequest.ForcedXenotype ?? XenotypeDefOf.Baseliner;
		xenotypeDef = (RaceRestrictionSettings.CanUseXenotype(xenotypeDef, kindDef.race) ? xenotypeDef : (RaceRestrictionSettings.FilterXenotypes(DefDatabase<XenotypeDef>.AllDefsListForReading, kindDef.race, out var _).TryRandomElement(out var def) ? def : xenotypeDef));
		xenotypeCustom = currentStartingRequest.ForcedCustomXenotype;
		devStage = (currentStartingRequest.AllowedDevelopmentalStages.Equals(DevelopmentalStage.None) ? DevelopmentalStage.Adult : currentStartingRequest.AllowedDevelopmentalStages);
		allowDowned = currentStartingRequest.AllowDowned;
		if (!CachedData.canBeChild(kindDef))
		{
			devStage = DevelopmentalStage.Adult;
			allowDowned = false;
		}
	}

	public static IEnumerable<PawnKindEntry> NewGeneratedStartingPawnKinds(PawnKindDef basicMember)
	{
		return (from sce in DefDatabase<RaceSettings>.AllDefsListForReading.Where((RaceSettings tdar) => !tdar.pawnKindSettings.startingColonists.NullOrEmpty()).SelectMany((RaceSettings tdar) => tdar.pawnKindSettings.startingColonists)
			where sce.factionDefs.Contains(Faction.OfPlayer.def)
			select sce).SelectMany((FactionPawnKindEntry sce) => sce.pawnKindEntries).AddItem(new PawnKindEntry
		{
			chance = 100f,
			kindDefs = new List<PawnKindDef>(1) { basicMember }
		});
	}

	public static PawnKindDef NewGeneratedStartingPawnHelper(PawnKindDef basicMember)
	{
		IEnumerable<PawnKindEntry> usableEntries = NewGeneratedStartingPawnKinds(basicMember);
		if (startingPawnKindRestriction != null && usableEntries.Any((PawnKindEntry pke) => pke.kindDefs.Contains(startingPawnKindRestriction)))
		{
			return startingPawnKindRestriction;
		}
		if (!usableEntries.TryRandomElementByWeight((PawnKindEntry pke) => pke.chance, out var pk))
		{
			return basicMember;
		}
		return pk.kindDefs.RandomElement();
	}

	public static void RandomHediffsToGainOnBirthdayPostfix(ref IEnumerable<HediffGiver_Birthday> __result, ThingDef raceDef)
	{
		ThingDef_AlienRace obj = raceDef as ThingDef_AlienRace;
		if (obj != null && obj.alienRace.generalSettings.immuneToAge)
		{
			__result = new List<HediffGiver_Birthday>();
		}
	}

	public static bool GenerateRandomOldAgeInjuriesPrefix(Pawn pawn)
	{
		if (pawn.def is ThingDef_AlienRace alienProps && alienProps.alienRace.generalSettings.immuneToAge)
		{
			return false;
		}
		return true;
	}

	public static bool FillBackstoryInSlotShuffledPrefix(Pawn pawn, BackstorySlot slot, List<BackstoryCategoryFilter> backstoryCategories)
	{
		bioReference = null;
		if (slot == BackstorySlot.Adulthood && pawn.story.Childhood is AlienBackstoryDef { linkedBackstory: not null } absd)
		{
			pawn.story.Adulthood = absd.linkedBackstory;
			return false;
		}
		return true;
	}

	public static IEnumerable<CodeInstruction> FillBackstorySlotShuffledTranspiler(IEnumerable<CodeInstruction> instructions)
	{
		MethodInfo backstoryDatabaseInfo = AccessTools.PropertyGetter(typeof(DefDatabase<BackstoryDef>), "AllDefs");
		bool done = false;
		List<CodeInstruction> instructionList = instructions.ToList();
		for (int i = 0; i < instructionList.Count; i++)
		{
			CodeInstruction codeInstruction = instructionList[i];
			yield return codeInstruction;
			if (!done && i > 1 && codeInstruction.Calls(backstoryDatabaseInfo))
			{
				done = true;
				yield return new CodeInstruction(OpCodes.Ldarg_0);
				yield return new CodeInstruction(OpCodes.Ldarg_1);
				yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(patchType, "FilterBackstories"));
			}
		}
	}

	public static IEnumerable<BackstoryDef> FilterBackstories(IEnumerable<BackstoryDef> backstories, Pawn pawn, BackstorySlot slot)
	{
		return backstories.Where((BackstoryDef bs) => !(bs is AlienBackstoryDef alienBackstoryDef) || (alienBackstoryDef.Approved(pawn) && (slot != BackstorySlot.Adulthood || alienBackstoryDef.linkedBackstory == null || pawn.story.Childhood == alienBackstoryDef.linkedBackstory)));
	}

	public static void TryGetRandomUnusedSolidBioForPostfix(List<BackstoryCategoryFilter> backstoryCategories, ref bool __result, ref PawnBio result, PawnKindDef kind, Gender gender, string requiredLastName)
	{
		List<BackstoryCategoryFilter> categories = backstoryCategories.ListFullCopy();
		while (!categories.NullOrEmpty())
		{
			if (!categories.TryRandomElementByWeight((BackstoryCategoryFilter b) => b.commonality, out var bcf))
			{
				bcf = categories.RandomElement();
			}
			categories.Remove(bcf);
			if (SolidBioDatabase.allBios.Where(delegate(PawnBio pb)
			{
				ThingDef_AlienRace obj = kind.race as ThingDef_AlienRace;
				if (obj == null || obj.alienRace.generalSettings.allowHumanBios)
				{
					Info modExtension = kind.GetModExtension<Info>();
					if (modExtension == null || modExtension.allowHumanBios)
					{
						goto IL_0094;
					}
				}
				PawnBioDef pawnBioDef = DefDatabase<PawnBioDef>.AllDefs.FirstOrDefault((PawnBioDef pbd) => pb.name.ConfusinglySimilarTo(pbd.name));
				if (pawnBioDef != null && pawnBioDef.validRaces.Contains(kind.race))
				{
					goto IL_0094;
				}
				goto IL_0130;
				IL_0130:
				return false;
				IL_0094:
				if (pb.gender.IsGenderApplicable(gender) && (requiredLastName.NullOrEmpty() || pb.name.Last == requiredLastName) && (!kind.factionLeader || pb.pirateKing) && bcf.Matches(pb.adulthood))
				{
					return !pb.name.UsedThisGame;
				}
				goto IL_0130;
			}).TryRandomElement(out var bio))
			{
				result = bio;
				bioReference = DefDatabase<PawnBioDef>.AllDefs.FirstOrDefault((PawnBioDef pbd) => bio.name.ConfusinglySimilarTo(pbd.name));
				__result = true;
				return;
			}
		}
		result = null;
		__result = false;
	}

	public static void GenerateTraitsPostfix(Pawn pawn, PawnGenerationRequest request)
	{
		if (!request.AllowedDevelopmentalStages.Newborn() && request.CanGeneratePawnRelations)
		{
			CachedData.generatePawnsRelations(pawn, ref request);
		}
		if (!(pawn.def is ThingDef_AlienRace alienProps))
		{
			return;
		}
		List<AlienChanceEntry<TraitWithDegree>> alienTraits = new List<AlienChanceEntry<TraitWithDegree>>();
		if (!alienProps.alienRace.generalSettings.forcedRaceTraitEntries.NullOrEmpty())
		{
			alienTraits.AddRange(alienProps.alienRace.generalSettings.forcedRaceTraitEntries);
		}
		foreach (BackstoryDef backstory in pawn.story.AllBackstories)
		{
			if (backstory is AlienBackstoryDef alienBackstory && !alienBackstory.forcedTraitsChance.NullOrEmpty())
			{
				alienTraits.AddRange(alienBackstory.forcedTraitsChance);
			}
		}
		foreach (AlienChanceEntry<TraitWithDegree> ate in alienTraits)
		{
			foreach (TraitWithDegree trait in ate.Select(pawn))
			{
				if (!pawn.story.traits.HasTrait(trait.def))
				{
					pawn.story.traits.GainTrait(new Trait(trait.def, trait.degree, forced: true));
				}
			}
		}
		int traits = alienProps.alienRace.generalSettings.additionalTraits.RandomInRange;
		if (traits <= 0)
		{
			return;
		}
		List<Trait> traitList = PawnGenerator.GenerateTraitsFor(pawn, traits, request);
		foreach (Trait trait2 in traitList)
		{
			pawn.story.traits.GainTrait(trait2);
		}
	}

	public static IEnumerable<CodeInstruction> SkinColorTranspiler(IEnumerable<CodeInstruction> instructions)
	{
		MethodInfo baseColorInfo = AccessTools.PropertyGetter(typeof(Pawn_StoryTracker), "SkinColorBase");
		foreach (CodeInstruction instruction in instructions)
		{
			if (instruction.Calls(baseColorInfo))
			{
				yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Pawn_StoryTracker), "pawn"));
				yield return CodeInstruction.Call(patchType, "SkinColorHelper");
			}
			else
			{
				yield return instruction;
			}
		}
	}

	public static Color SkinColorHelper(Pawn pawn)
	{
		if (pawn.def is ThingDef_AlienRace alienProps)
		{
			return alienProps.alienRace.generalSettings.alienPartGenerator.SkinColor(pawn);
		}
		return CachedData.skinColorBase(pawn.story) ?? pawn.story.SkinColorBase;
	}

	public static void GetBodyTypeForPostfix(Pawn pawn, ref BodyTypeDef __result)
	{
		__result = CheckBodyType(pawn, __result);
	}

	public static void GenerateBodyTypePostfix(Pawn pawn)
	{
		pawn.story.bodyType = CheckBodyType(pawn, pawn.story.bodyType);
	}

	public static BodyTypeDef CheckBodyType(Pawn pawn, BodyTypeDef bodyType)
	{
		if (AlienBackstoryDef.checkBodyType.Contains(pawn.story.GetBackstory(BackstorySlot.Adulthood)))
		{
			bodyType = DefDatabase<BodyTypeDef>.GetRandom();
		}
		if (pawn.def is ThingDef_AlienRace alienProps)
		{
			AlienPartGenerator parts = alienProps.alienRace.generalSettings.alienPartGenerator;
			if (parts != null && !alienProps.alienRace.generalSettings.alienPartGenerator.bodyTypes.NullOrEmpty())
			{
				List<BodyTypeDef> bodyTypeDefs = parts.bodyTypes.ListFullCopy();
				if ((pawn.ageTracker.CurLifeStage.developmentalStage.Baby() || pawn.ageTracker.CurLifeStage.developmentalStage.Newborn()) && bodyTypeDefs.Contains(BodyTypeDefOf.Baby))
				{
					bodyType = BodyTypeDefOf.Baby;
				}
				else if (pawn.ageTracker.CurLifeStage.developmentalStage.Juvenile() && bodyTypeDefs.Contains(BodyTypeDefOf.Child))
				{
					bodyType = BodyTypeDefOf.Child;
				}
				else
				{
					bodyTypeDefs.Remove(BodyTypeDefOf.Baby);
					bodyTypeDefs.Remove(BodyTypeDefOf.Child);
					if (pawn.gender == Gender.Male)
					{
						BodyTypeDef femaleBodyType = parts.defaultFemaleBodyType;
						if (bodyTypeDefs.Contains(femaleBodyType) && bodyTypeDefs.Count > 1)
						{
							bodyTypeDefs.Remove(femaleBodyType);
						}
					}
					if (pawn.gender == Gender.Female)
					{
						BodyTypeDef maleBodyType = parts.defaultMaleBodyType;
						if (bodyTypeDefs.Contains(maleBodyType) && bodyTypeDefs.Count > 1)
						{
							bodyTypeDefs.Remove(maleBodyType);
						}
					}
					if (!bodyTypeDefs.Contains(bodyType))
					{
						bodyType = bodyTypeDefs.RandomElement();
					}
				}
			}
		}
		return bodyType;
	}

	public static void GeneratePawnPrefix(ref PawnGenerationRequest request)
	{
		PawnKindDef kindDef = request.KindDef;
		if (request.AllowedDevelopmentalStages.Newborn())
		{
			return;
		}
		if (Faction.OfPlayerSilentFail != null && kindDef == PawnKindDefOf.Colonist)
		{
			Faction faction = request.Faction;
			if (faction != null && faction.IsPlayer && kindDef.race != Faction.OfPlayer?.def.basicMemberKind.race)
			{
				kindDef = Faction.OfPlayer?.def.basicMemberKind ?? request.KindDef;
			}
		}
		IEnumerable<RaceSettings> settings = DefDatabase<RaceSettings>.AllDefsListForReading;
		PawnKindEntry pk;
		if (request.KindDef == PawnKindDefOf.SpaceRefugee || request.KindDef == PawnKindDefOf.Refugee)
		{
			if (settings.Where((RaceSettings r) => !r.pawnKindSettings.alienrefugeekinds.NullOrEmpty()).SelectMany((RaceSettings r) => r.pawnKindSettings.alienrefugeekinds).TryRandomElementByWeight((PawnKindEntry pke) => pke.chance, out pk))
			{
				kindDef = pk.kindDefs.RandomElement();
			}
		}
		else if (request.KindDef == PawnKindDefOf.Slave && settings.Where((RaceSettings r) => !r.pawnKindSettings.alienslavekinds.NullOrEmpty()).SelectMany((RaceSettings r) => r.pawnKindSettings.alienslavekinds).TryRandomElementByWeight((PawnKindEntry pke) => pke.chance, out pk))
		{
			kindDef = pk.kindDefs.RandomElement();
		}
		if (request.ForcedXenotype != null && !RaceRestrictionSettings.CanUseXenotype(request.ForcedXenotype, kindDef.race))
		{
			kindDef = request.KindDef;
		}
		request.KindDef = kindDef;
	}

	public static void GeneratePawnPostfix(Pawn __result)
	{
		if (!(__result?.def is ThingDef_AlienRace race))
		{
			return;
		}
		foreach (AlienChanceEntry<AbilityDef> entry in race.alienRace.generalSettings.abilities)
		{
			foreach (AbilityDef ability in entry.Select(__result))
			{
				__result.abilities?.GainAbility(ability);
			}
		}
	}

	public static IEnumerable<CodeInstruction> TryGetGraphicApparelTranspiler(IEnumerable<CodeInstruction> codeInstructions)
	{
		MethodInfo originalMethod = AccessTools.Method(typeof(GraphicDatabase), "Get", new Type[4]
		{
			typeof(string),
			typeof(Shader),
			typeof(Vector2),
			typeof(Color)
		}, new Type[1] { typeof(Graphic_Multi) });
		MethodInfo newMethod = AccessTools.Method(typeof(ApparelGraphicUtility), "GetGraphic");
		foreach (CodeInstruction instruction in codeInstructions)
		{
			if (instruction.Calls(originalMethod))
			{
				yield return new CodeInstruction(OpCodes.Ldarg_0).MoveLabelsFrom(instruction);
				yield return new CodeInstruction(OpCodes.Ldarg_1);
				yield return new CodeInstruction(OpCodes.Call, newMethod);
			}
			else
			{
				yield return instruction;
			}
		}
	}
}
