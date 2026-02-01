using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded;

[HarmonyPatch(typeof(PawnGenerator), "GenerateNewPawnInternal")]
[HarmonyAfter(new string[] { "OskarPotocki.VEF" })]
public class PawnGen_Patch
{
	[HarmonyPostfix]
	public static void Postfix(Pawn __result, PawnGenerationRequest request)
	{
		if (__result == null || request.AllowedDevelopmentalStages.Newborn())
		{
			return;
		}
		PawnKindAbilityExtension_Psycasts modExtension = ((Def)__result.kindDef).GetModExtension<PawnKindAbilityExtension_Psycasts>();
		CompAbilities comp = null;
		if (modExtension != null)
		{
			comp = ((ThingWithComps)__result).GetComp<CompAbilities>();
			if (((PawnKindAbilityExtension)modExtension).implantDef != null)
			{
				Hediff_Psylink hediff_Psylink = __result.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PsychicAmplifier) as Hediff_Psylink;
				if (hediff_Psylink == null)
				{
					hediff_Psylink = HediffMaker.MakeHediff(HediffDefOf.PsychicAmplifier, __result, __result.health.hediffSet.GetBrain()) as Hediff_Psylink;
					__result.health.AddHediff(hediff_Psylink);
				}
				Hediff_PsycastAbilities hediff_PsycastAbilities = __result.health.hediffSet.GetFirstHediffOfDef(((PawnKindAbilityExtension)modExtension).implantDef) as Hediff_PsycastAbilities;
				if (hediff_PsycastAbilities.psylink == null)
				{
					hediff_PsycastAbilities.InitializeFromPsylink(hediff_Psylink);
				}
				foreach (PathUnlockData unlockedPath in modExtension.unlockedPaths)
				{
					if (unlockedPath.path.CanPawnUnlock(__result))
					{
						hediff_PsycastAbilities.UnlockPath(unlockedPath.path);
						int num = unlockedPath.unlockedAbilityCount.RandomInRange;
						IEnumerable<AbilityDef> enumerable = new List<AbilityDef>();
						for (int i = unlockedPath.unlockedAbilityLevelRange.min; i < unlockedPath.unlockedAbilityLevelRange.max && i < unlockedPath.path.MaxLevel; i++)
						{
							enumerable = enumerable.Concat(unlockedPath.path.abilityLevelsInOrder[i - 1].Except(PsycasterPathDef.Blank));
						}
						List<AbilityDef> list = enumerable.ToList();
						List<AbilityDef> source;
						while ((source = list.Where((AbilityDef ab) => ab.Psycast().PrereqsCompleted(comp)).ToList()).Any() && num > 0)
						{
							num--;
							AbilityDef val = source.RandomElement();
							comp.GiveAbility(val);
							hediff_PsycastAbilities.ChangeLevel(1, sendLetter: false);
							hediff_PsycastAbilities.points--;
							list.Remove(val);
						}
					}
				}
				int randomInRange = modExtension.statUpgradePoints.RandomInRange;
				((Hediff_Level)(object)hediff_PsycastAbilities).ChangeLevel(randomInRange);
				hediff_PsycastAbilities.points -= randomInRange;
				hediff_PsycastAbilities.ImproveStats(randomInRange);
			}
		}
		if (Find.Storyteller?.def != VPE_DefOf.VPE_Basilicus || (int)__result.RaceProps.intelligence < 2 || !(Rand.Value < PsycastsMod.Settings.baseSpawnChance))
		{
			return;
		}
		Hediff_Psylink hediff_Psylink2 = __result.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PsychicAmplifier) as Hediff_Psylink;
		if (hediff_Psylink2 == null)
		{
			hediff_Psylink2 = HediffMaker.MakeHediff(HediffDefOf.PsychicAmplifier, __result, __result.health.hediffSet.GetBrain()) as Hediff_Psylink;
			__result.health.AddHediff(hediff_Psylink2);
		}
		Hediff_PsycastAbilities hediff_PsycastAbilities2 = (__result.health.hediffSet.GetFirstHediffOfDef(VPE_DefOf.VPE_PsycastAbilityImplant) as Hediff_PsycastAbilities) ?? (HediffMaker.MakeHediff(VPE_DefOf.VPE_PsycastAbilityImplant, __result, __result.RaceProps.body.GetPartsWithDef(VPE_DefOf.Brain).FirstOrFallback()) as Hediff_PsycastAbilities);
		if (hediff_PsycastAbilities2.psylink == null)
		{
			hediff_PsycastAbilities2.InitializeFromPsylink(hediff_Psylink2);
		}
		PsycasterPathDef psycasterPathDef = DefDatabase<PsycasterPathDef>.AllDefsListForReading.Where((PsycasterPathDef ppd) => ppd.CanPawnUnlock(__result)).RandomElement();
		hediff_PsycastAbilities2.UnlockPath(psycasterPathDef);
		if (comp == null)
		{
			comp = ((ThingWithComps)__result).GetComp<CompAbilities>();
		}
		IEnumerable<AbilityDef> enumerable2 = psycasterPathDef.abilities.Except(comp.LearnedAbilities.Select((Ability ab) => ab.def));
		AbilityDef result;
		while (enumerable2.Where((AbilityDef ab) => ((Def)(object)ab).GetModExtension<AbilityExtension_Psycast>().PrereqsCompleted(comp)).TryRandomElement(out result))
		{
			comp.GiveAbility(result);
			if (hediff_PsycastAbilities2.points <= 0)
			{
				hediff_PsycastAbilities2.ChangeLevel(1, sendLetter: false);
			}
			hediff_PsycastAbilities2.points--;
			enumerable2 = enumerable2.Except(result);
			if (!(Rand.Value < PsycastsMod.Settings.additionalAbilityChance))
			{
				break;
			}
		}
	}
}
