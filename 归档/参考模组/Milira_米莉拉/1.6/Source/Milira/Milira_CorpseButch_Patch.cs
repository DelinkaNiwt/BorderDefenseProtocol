using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Milira;

[HarmonyPatch(typeof(Corpse))]
[HarmonyPatch("ButcherProducts")]
public static class Milira_CorpseButch_Patch
{
	[HarmonyPostfix]
	[HarmonyPriority(100)]
	public static IEnumerable<Thing> Postfix(IEnumerable<Thing> __result, Corpse __instance, Pawn butcher, float efficiency)
	{
		foreach (Thing item in __result)
		{
			yield return item;
		}
		Pawn pawn = __instance.InnerPawn;
		SimpleCurve skillLevelToProductChanceCurve = new SimpleCurve
		{
			new CurvePoint(0f, 0f),
			new CurvePoint(5f, 0f),
			new CurvePoint(20f, 0.6f)
		};
		if (MilianUtility.IsMilian(pawn) && ((butcher.RaceProps.Humanlike && butcher.skills.GetSkill(SkillDefOf.Crafting) != null && Rand.Chance(skillLevelToProductChanceCurve.Evaluate(butcher.skills.GetSkill(SkillDefOf.Crafting).Level))) || (butcher.def.defName == "Milira_Race" && butcher.skills.GetSkill(SkillDefOf.Crafting).Level > 6) || (!butcher.RaceProps.Humanlike && Rand.Chance(0.1f))))
		{
			if (MilianUtility.IsMilian_PawnClass(pawn))
			{
				yield return ThingMaker.MakeThing(MiliraDefOf.Milian_NamePlate_Pawn);
			}
			else if (MilianUtility.IsMilian_KnightClass(pawn))
			{
				yield return ThingMaker.MakeThing(MiliraDefOf.Milian_NamePlate_Knight);
			}
			else if (MilianUtility.IsMilian_BishopClass(pawn))
			{
				yield return ThingMaker.MakeThing(MiliraDefOf.Milian_NamePlate_Bishop);
			}
			else if (MilianUtility.IsMilian_RookClass(pawn))
			{
				yield return ThingMaker.MakeThing(MiliraDefOf.Milian_NamePlate_Rook);
			}
			else if (pawn.def.defName == "Milian_Mechanoid_Queen")
			{
				yield return ThingMaker.MakeThing(MiliraDefOf.Milian_NamePlate_Queen);
			}
			else if (pawn.def.defName == "Milian_Mechanoid_King")
			{
				yield return ThingMaker.MakeThing(MiliraDefOf.Milian_NamePlate_King);
			}
			if (Rand.Chance(0.15f))
			{
				yield return ThingMaker.MakeThing(MiliraDefOf.Milira_SolarCrystal);
			}
		}
	}
}
