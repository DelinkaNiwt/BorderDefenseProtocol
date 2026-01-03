using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Milira;

[HarmonyPatch(new Type[] { typeof(PawnGenerationRequest) })]
[HarmonyPatch(typeof(PawnGenerator))]
[HarmonyPatch("GeneratePawn")]
public static class Milira_MilianPawnGenerator_Patch
{
	private static readonly SimpleCurve MilianDifficultyToEliteChance = new SimpleCurve
	{
		new CurvePoint(0f, 0.01f),
		new CurvePoint(1f, 0.02f),
		new CurvePoint(2.2f, 0.06f),
		new CurvePoint(5f, 0.2f)
	};

	[HarmonyPostfix]
	public static void Postfix(ref Pawn __result, PawnGenerationRequest request)
	{
		if (!MilianUtility.IsMilian(__result))
		{
			return;
		}
		PawnKindDef kindDef = request.KindDef;
		List<ThingDef> apparelRequired = kindDef.apparelRequired;
		if (!kindDef.apparelTags.NullOrEmpty())
		{
			foreach (string tag in kindDef.apparelTags)
			{
				ThingDef thingDef = DefDatabase<ThingDef>.AllDefsListForReading.Where(delegate(ThingDef t)
				{
					ApparelProperties apparel3 = t.apparel;
					return apparel3 != null && apparel3.tags?.Contains(tag) == true;
				}).RandomElementByWeight((ThingDef t) => t.generateCommonality);
				if (thingDef != null)
				{
					apparelRequired.Add(thingDef);
				}
			}
		}
		if (apparelRequired == null)
		{
			return;
		}
		if (request.Faction?.def?.defName == "Milira_Faction")
		{
			float threatScale = Find.Storyteller.difficulty.threatScale;
			Map map = Find.CurrentMap;
			if (map != null && map.IsPocketMap)
			{
				map = Find.AnyPlayerHomeMap;
			}
			float playerWealthForStoryteller = map.PlayerWealthForStoryteller;
			if (MiliraRaceSettings.MiliraRace_ModSetting_MilianDifficulty_EquipmentMaterial && Rand.Chance(MilianDifficultyToEliteChance.Evaluate(threatScale)))
			{
				if (MiliraRaceSettings.MiliraRace_ModSetting_MilianDifficulty_EquipmentQuality)
				{
					PawnGenerationRequest request2 = request;
					bool forceNormalGearQuality = true;
					QualityCategory value = QualityCategory.Normal;
					ThingDef milira_SplendidSteel = MiliraDefOf.Milira_SplendidSteel;
					request2.KindDef.forceNormalGearQuality = false;
					if (MilianUtility.IsMilian_KnightClass(__result) || MilianUtility.IsMilian_RookClass(__result))
					{
						request2.KindDef.weaponStuffOverride = MiliraDefOf.Milira_SunPlateSteel;
					}
					if (playerWealthForStoryteller > 300000f && playerWealthForStoryteller <= 600000f)
					{
						request2.KindDef.forceWeaponQuality = QualityCategory.Good;
					}
					else if (playerWealthForStoryteller > 600000f && playerWealthForStoryteller <= 900000f)
					{
						request2.KindDef.forceWeaponQuality = QualityCategory.Excellent;
					}
					else if (playerWealthForStoryteller > 900000f && playerWealthForStoryteller <= 1500000f)
					{
						request2.KindDef.forceWeaponQuality = QualityCategory.Masterwork;
					}
					else if (playerWealthForStoryteller > 1500000f)
					{
						request2.KindDef.forceWeaponQuality = QualityCategory.Legendary;
					}
					else
					{
						request2.KindDef.forceWeaponQuality = QualityCategory.Normal;
					}
					PawnWeaponGenerator.TryGenerateWeaponFor(__result, request2);
					request2.KindDef.forceNormalGearQuality = forceNormalGearQuality;
					request2.KindDef.forceWeaponQuality = value;
					if (MilianUtility.IsMilian_KnightClass(__result) || MilianUtility.IsMilian_RookClass(__result))
					{
						request2.KindDef.weaponStuffOverride = milira_SplendidSteel;
					}
				}
				for (int num = 0; num < apparelRequired.Count; num++)
				{
					Apparel apparel = (Apparel)ThingMaker.MakeThing(apparelRequired[num], GenStuff.DefaultStuffFor(apparelRequired[num]));
					if (MiliraDefOf.Milira_SunPlateSteel.stuffProps.CanMake(apparelRequired[num]))
					{
						apparel = (Apparel)ThingMaker.MakeThing(apparelRequired[num], MiliraDefOf.Milira_SunPlateSteel);
					}
					if (MiliraDefOf.Milira_Feather.stuffProps.CanMake(apparelRequired[num]))
					{
						apparel = (Apparel)ThingMaker.MakeThing(apparelRequired[num], MiliraDefOf.Milira_Feather);
					}
					if (MiliraRaceSettings.MiliraRace_ModSetting_MilianDifficulty_EquipmentQuality)
					{
						QualityCategory q = QualityCategory.Normal;
						if (playerWealthForStoryteller > 300000f && playerWealthForStoryteller <= 600000f)
						{
							q = QualityCategory.Good;
						}
						else if (playerWealthForStoryteller > 600000f && playerWealthForStoryteller <= 900000f)
						{
							q = QualityCategory.Excellent;
						}
						else if (playerWealthForStoryteller > 900000f && playerWealthForStoryteller <= 1500000f)
						{
							q = QualityCategory.Masterwork;
						}
						else if (playerWealthForStoryteller > 1500000f)
						{
							q = QualityCategory.Legendary;
						}
						apparel.TryGetComp<CompQuality>()?.SetQuality(q, ArtGenerationContext.Colony);
					}
					__result.apparel.Wear(apparel, dropReplacedApparel: true, locked: true);
				}
				return;
			}
			if (MiliraRaceSettings.MiliraRace_ModSetting_MilianDifficulty_EquipmentQuality)
			{
				PawnGenerationRequest request3 = request;
				bool forceNormalGearQuality2 = true;
				QualityCategory value2 = QualityCategory.Normal;
				request3.KindDef.forceNormalGearQuality = false;
				if (playerWealthForStoryteller > 300000f && playerWealthForStoryteller <= 600000f)
				{
					request3.KindDef.forceWeaponQuality = QualityCategory.Good;
				}
				else if (playerWealthForStoryteller > 600000f && playerWealthForStoryteller <= 900000f)
				{
					request3.KindDef.forceWeaponQuality = QualityCategory.Excellent;
				}
				else if (playerWealthForStoryteller > 900000f && playerWealthForStoryteller <= 1500000f)
				{
					request3.KindDef.forceWeaponQuality = QualityCategory.Masterwork;
				}
				else if (playerWealthForStoryteller > 1500000f)
				{
					request3.KindDef.forceWeaponQuality = QualityCategory.Legendary;
				}
				else
				{
					request3.KindDef.forceWeaponQuality = QualityCategory.Normal;
				}
				PawnWeaponGenerator.TryGenerateWeaponFor(__result, request3);
				request3.KindDef.forceNormalGearQuality = forceNormalGearQuality2;
				request3.KindDef.forceWeaponQuality = value2;
			}
			for (int num2 = 0; num2 < apparelRequired.Count; num2++)
			{
				Apparel apparel2 = (Apparel)ThingMaker.MakeThing(apparelRequired[num2], GenStuff.DefaultStuffFor(apparelRequired[num2]));
				if (MiliraDefOf.Milira_SplendidSteel.stuffProps.CanMake(apparelRequired[num2]))
				{
					apparel2 = (Apparel)ThingMaker.MakeThing(apparelRequired[num2], MiliraDefOf.Milira_SplendidSteel);
				}
				if (MiliraDefOf.Milira_FeatherThread.stuffProps.CanMake(apparelRequired[num2]))
				{
					apparel2 = (Apparel)ThingMaker.MakeThing(apparelRequired[num2], MiliraDefOf.Milira_FeatherThread);
				}
				if (MiliraRaceSettings.MiliraRace_ModSetting_MilianDifficulty_EquipmentQuality)
				{
					QualityCategory q2 = QualityCategory.Normal;
					if (playerWealthForStoryteller > 300000f && playerWealthForStoryteller <= 600000f)
					{
						q2 = QualityCategory.Good;
					}
					else if (playerWealthForStoryteller > 600000f && playerWealthForStoryteller <= 900000f)
					{
						q2 = QualityCategory.Excellent;
					}
					else if (playerWealthForStoryteller > 900000f && playerWealthForStoryteller <= 1500000f)
					{
						q2 = QualityCategory.Masterwork;
					}
					else if (playerWealthForStoryteller > 1500000f)
					{
						q2 = QualityCategory.Legendary;
					}
					apparel2.TryGetComp<CompQuality>()?.SetQuality(q2, ArtGenerationContext.Colony);
				}
				__result.apparel.Wear(apparel2, dropReplacedApparel: true, locked: true);
			}
			return;
		}
		for (int num3 = 0; num3 < apparelRequired.Count; num3++)
		{
			Apparel newApparel = (Apparel)ThingMaker.MakeThing(apparelRequired[num3], GenStuff.DefaultStuffFor(apparelRequired[num3]));
			if (MiliraDefOf.Milira_SplendidSteel.stuffProps.CanMake(apparelRequired[num3]))
			{
				newApparel = (Apparel)ThingMaker.MakeThing(apparelRequired[num3], MiliraDefOf.Milira_SplendidSteel);
			}
			if (MiliraDefOf.Milira_FeatherThread.stuffProps.CanMake(apparelRequired[num3]))
			{
				newApparel = (Apparel)ThingMaker.MakeThing(apparelRequired[num3], MiliraDefOf.Milira_FeatherThread);
			}
			__result.apparel.Wear(newApparel, dropReplacedApparel: true, locked: true);
		}
	}
}
