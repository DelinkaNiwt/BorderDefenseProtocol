using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public static class BulletUtility
{
	public static float GetHitChanceFactor(Thing equipment, float dist)
	{
		float value = ((dist <= 3f) ? AdjustedAccuracy(RangeCategory.Touch, equipment) : ((dist <= 12f) ? Mathf.Lerp(AdjustedAccuracy(RangeCategory.Touch, equipment), AdjustedAccuracy(RangeCategory.Short, equipment), (dist - 3f) / 9f) : ((dist <= 25f) ? Mathf.Lerp(AdjustedAccuracy(RangeCategory.Short, equipment), AdjustedAccuracy(RangeCategory.Medium, equipment), (dist - 12f) / 13f) : ((!(dist <= 40f)) ? AdjustedAccuracy(RangeCategory.Long, equipment) : Mathf.Lerp(AdjustedAccuracy(RangeCategory.Medium, equipment), AdjustedAccuracy(RangeCategory.Long, equipment), (dist - 25f) / 15f)))));
		return Mathf.Clamp(value, 0.01f, 1f);
	}

	private static float AdjustedAccuracy(RangeCategory cat, Thing equipment)
	{
		if (equipment != null)
		{
			StatDef stat = null;
			switch (cat)
			{
			case RangeCategory.Touch:
				stat = StatDefOf.AccuracyTouch;
				break;
			case RangeCategory.Short:
				stat = StatDefOf.AccuracyShort;
				break;
			case RangeCategory.Medium:
				stat = StatDefOf.AccuracyMedium;
				break;
			case RangeCategory.Long:
				stat = StatDefOf.AccuracyLong;
				break;
			}
			return equipment.GetStatValue(stat);
		}
		return 1f;
	}

	public static List<CoverInfo> Targetcovers(Thing caster, LocalTargetInfo target, out float coversOverallBlockChance)
	{
		List<CoverInfo> result = CoverUtility.CalculateCoverGiverSet(target, caster.Position, caster.Map);
		coversOverallBlockChance = CoverUtility.CalculateOverallBlockChance(target, caster.Position, caster.Map);
		return result;
	}

	public static Thing GetRandomCoverToMissInto(List<CoverInfo> covers)
	{
		if (covers.TryRandomElementByWeight((CoverInfo c) => c.BlockChance, out var result))
		{
			return result.Thing;
		}
		return null;
	}

	public static float GetPierceHitChance(Thing caster, Thing equipment, Pawn targetpawn, Vector3 origin)
	{
		float num = 1f;
		float magnitude = (origin.ToIntVec3() - targetpawn.Position).Magnitude;
		if (equipment != null)
		{
			float hitChanceFactor = GetHitChanceFactor(equipment, magnitude);
			num *= hitChanceFactor;
		}
		float num2 = 1f * Mathf.Clamp(targetpawn.BodySize, 0.1f, 2f);
		if (targetpawn.GetPosture() != PawnPosture.Standing)
		{
			num2 *= 0.1f;
		}
		return num * num2;
	}
}
