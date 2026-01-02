using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Milira;

public class CompSendPromotionRequest : ThingComp
{
	private float closestDistance = float.MaxValue;

	private Pawn closestPawn;

	public CompProperties_SendPromotionRequest Props => (CompProperties_SendPromotionRequest)props;

	public override void PostDestroy(DestroyMode mode, Map previousMap)
	{
		if (parent.Faction != Faction.OfPlayer && MiliraRaceSettings.MiliraRace_ModSetting_MilianDifficulty_Promotion && previousMap != null)
		{
			SendPromotionToPawnClosest(previousMap);
		}
		base.PostDestroy(mode, previousMap);
	}

	private bool IsPawnAffected(Pawn target)
	{
		Faction faction = Find.FactionManager.FirstFactionOfDef(MiliraDefOf.Milira_Faction);
		if (target.Dead || target.health == null)
		{
			return false;
		}
		if (target.Faction != parent.Faction || parent.Faction != faction)
		{
			return false;
		}
		if (target.def.defName == "Milian_Mechanoid_PawnI" || target.def.defName == "Milian_Mechanoid_PawnII" || target.def.defName == "Milian_Mechanoid_PawnIII" || target.def.defName == "Milian_Mechanoid_PawnIV")
		{
			return true;
		}
		return false;
	}

	protected void SendPromotionToPawnClosest(Map map)
	{
		List<HediffDef> list = new List<HediffDef>
		{
			MiliraDefOf.Milian_PawnPromotion_BishopI,
			MiliraDefOf.Milian_PawnPromotion_BishopII,
			MiliraDefOf.Milian_PawnPromotion_BishopIII,
			MiliraDefOf.Milian_PawnPromotion_BishopIV,
			MiliraDefOf.Milian_PawnPromotion_KnightI,
			MiliraDefOf.Milian_PawnPromotion_KnightII,
			MiliraDefOf.Milian_PawnPromotion_KnightIII,
			MiliraDefOf.Milian_PawnPromotion_KnightIV,
			MiliraDefOf.Milian_PawnPromotion_RookI,
			MiliraDefOf.Milian_PawnPromotion_RookII,
			MiliraDefOf.Milian_PawnPromotion_RookIII,
			MiliraDefOf.Milian_PawnPromotion_RookIV,
			MiliraDefOf.Milian_PawnPromotion_Queen
		};
		foreach (Pawn item in map.mapPawns.AllPawnsSpawned)
		{
			if (IsPawnAffected(item))
			{
				float num = item.Position.DistanceTo(parent.Position);
				if (num < closestDistance && !list.Any((HediffDef hediffDef) => item.health.hediffSet.HasHediff(hediffDef)))
				{
					closestPawn = item;
					closestDistance = num;
				}
			}
		}
		if (closestPawn != null && (closestDistance < 40f || MiliraRaceSettings.MiliraRace_ModSetting_MilianDifficulty_WidePromotion))
		{
			BodyPartRecord bodyPartRecord = closestPawn.health.hediffSet.GetNotMissingParts().FirstOrDefault((BodyPartRecord x) => x.def.defName == "Milian_Brain");
			if (MiliraRaceSettings.MiliraRace_ModSetting_MilianDifficulty_FastPromotion)
			{
				HealthUtility.AdjustSeverity(closestPawn, Props.promotionHediffType, 0.4f);
			}
			else
			{
				HealthUtility.AdjustSeverity(closestPawn, Props.promotionHediffType, 0.01f);
			}
		}
	}
}
