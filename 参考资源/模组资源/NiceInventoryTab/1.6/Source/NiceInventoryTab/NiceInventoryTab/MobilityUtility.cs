using System;
using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace NiceInventoryTab;

public static class MobilityUtility
{
	private static List<(Thing, float)> apparelOffsetList = new List<(Thing, float)>();

	public static float MoveSpeed(Pawn pawn, StatDrawer statBar)
	{
		StatBar statBar2 = statBar as StatBar;
		if (pawn.Dead)
		{
			if (statBar2 != null)
			{
				statBar2.Descr = null;
			}
			return 0f;
		}
		if (statBar2 == null)
		{
			return pawn.GetStatValue(StatDefOf.MoveSpeed);
		}
		(float original, float filtered) statValueFilterHediffs = HediffUtility.GetStatValueFilterHediffs(pawn, StatDefOf.MoveSpeed, HediffUtility.IsPermanent_or_IsImplant);
		float item = statValueFilterHediffs.original;
		float item2 = statValueFilterHediffs.filtered;
		StatRequest req = StatRequest.For(pawn);
		apparelOffsetList.Clear();
		float num = CalcGearAffects(statBar2, pawn, ref apparelOffsetList);
		float val = 1f;
		StatDefOf.MoveSpeed.parts.FirstOrDefault((StatPart p) => p is StatPart_Glow)?.TransformValue(req, ref val);
		float num2 = item / val;
		float num3 = item - num2;
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("MoveSpeed".Translate());
		stringBuilder.Append("\n\n");
		stringBuilder.Append("StatsReport_FinalValue".Translate());
		stringBuilder.Append(": ");
		stringBuilder.Append(item.ToString(Assets.Format_MoveSpeed));
		if (apparelOffsetList.Count > 0)
		{
			stringBuilder.Append("\n\n");
			stringBuilder.Append("StatsReport_RelevantGear".Translate());
			foreach (var (thing, num4) in apparelOffsetList)
			{
				stringBuilder.Append("\n ");
				stringBuilder.Append(thing.LabelNoParenthesisCap + ": " + num4.ToString("0.##").Colorize(Assets.GoodOrBad(num4 > 0f)));
			}
		}
		if (!Mathf.Approximately(val, 1f))
		{
			stringBuilder.Append("\n\n");
			stringBuilder.Append("StatsReport_LightMultiplier".Translate(val.ToStringPercent()).Colorize(Assets.EnviromentPenaltyColor));
		}
		float num5 = PawnCapacityUtility.CalculateCapacityLevel(pawn.health.hediffSet, PawnCapacityDefOf.Moving);
		float num6 = item - item2;
		if (num6 < 0f)
		{
			num += num6;
		}
		else if (num6 > 0f && Settings.DrugImpactVisible)
		{
			statBar2.AddBuff(num6, statBar2.ColorBar);
		}
		if (num5 != 1f || num6 != 0f)
		{
			stringBuilder.Append("\n\n");
			stringBuilder.Append(GetPawnCapacityTipColored(pawn, PawnCapacityDefOf.Moving));
		}
		statBar2.Descr = stringBuilder.ToString();
		if (num3 < -0.1f)
		{
			statBar2.AddDebuff(num3, Assets.EnviromentPenaltyColor);
		}
		if (num < -0.1f)
		{
			statBar2.AddDebuff(num, Assets.PenaltyColor);
		}
		return item;
	}

	public static float ScaleFactor(float factor, float scale)
	{
		return 1f - (1f - factor) * scale;
	}

	private static string GetPawnRelevantHediffs(Pawn pawn, StatDef stat)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("StatsReport_RelevantHediffs".Translate());
		foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
		{
			HediffStage curStage = hediff.CurStage;
			if (curStage == null)
			{
				continue;
			}
			float num = curStage.statOffsets.GetStatOffsetFromList(stat);
			if (num != 0f)
			{
				if (curStage.statOffsetEffectMultiplier != null)
				{
					num *= pawn.GetStatValue(curStage.statOffsetEffectMultiplier);
				}
				if (curStage.multiplyStatChangesBySeverity)
				{
					num *= hediff.Severity;
				}
				stringBuilder.AppendLine("  " + hediff.LabelBaseCap + ": " + num.ToString("F2").Colorize(Assets.GoodOrBad(num > 0f)));
			}
			float num2 = curStage.statFactors.GetStatFactorFromList(stat);
			if (Math.Abs(num2 - 1f) > float.Epsilon)
			{
				if (curStage.multiplyStatChangesBySeverity)
				{
					num2 = ScaleFactor(num2, hediff.Severity);
				}
				if (curStage.statFactorEffectMultiplier != null)
				{
					num2 = ScaleFactor(num2, pawn.GetStatValue(curStage.statFactorEffectMultiplier));
				}
				stringBuilder.AppendLine("    " + hediff.LabelBaseCap + ": " + num2.ToStringByStyle(stat.ToStringStyleUnfinalized, ToStringNumberSense.Factor).Colorize(Assets.GoodOrBad(num2 > 1f)));
			}
		}
		return stringBuilder.ToString();
	}

	public static string GetPawnCapacityTipColored(Pawn pawn, PawnCapacityDef capacity)
	{
		List<Hediff> list = new List<Hediff>();
		List<BodyPartRecord> list2 = new List<BodyPartRecord>();
		List<Gene> list3 = new List<Gene>();
		List<PawnCapacityUtility.CapacityImpactor> list4 = new List<PawnCapacityUtility.CapacityImpactor>();
		float num = PawnCapacityUtility.CalculateCapacityLevel(pawn.health.hediffSet, capacity, list4);
		list4.RemoveAll((PawnCapacityUtility.CapacityImpactor x) => x is PawnCapacityUtility.CapacityImpactorCapacity capacityImpactorCapacity && !capacityImpactorCapacity.capacity.CanShowOnPawn(pawn));
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(capacity.GetLabelFor(pawn).CapitalizeFirst() + ": " + num.ToStringPercent().Colorize(Assets.GoodOrBadOrNeutral(num, 1f)));
		if (list4.Count > 0)
		{
			for (int num2 = 0; num2 < list4.Count; num2++)
			{
				if (list4[num2] is PawnCapacityUtility.CapacityImpactorHediff capacityImpactorHediff && !list.Contains(capacityImpactorHediff.hediff))
				{
					stringBuilder.AppendLine("  " + list4[num2].Readable(pawn));
					list.Add(capacityImpactorHediff.hediff);
				}
			}
			list.Clear();
			for (int num3 = 0; num3 < list4.Count; num3++)
			{
				if (list4[num3] is PawnCapacityUtility.CapacityImpactorBodyPartHealth capacityImpactorBodyPartHealth && !list2.Contains(capacityImpactorBodyPartHealth.bodyPart))
				{
					stringBuilder.AppendLine("  " + list4[num3].Readable(pawn));
					list2.Add(capacityImpactorBodyPartHealth.bodyPart);
				}
			}
			list2.Clear();
			for (int num4 = 0; num4 < list4.Count; num4++)
			{
				if (list4[num4] is PawnCapacityUtility.CapacityImpactorGene capacityImpactorGene && !list3.Contains(capacityImpactorGene.gene))
				{
					stringBuilder.AppendLine("  " + list4[num4].Readable(pawn));
					list3.Add(capacityImpactorGene.gene);
				}
			}
			list3.Clear();
			for (int num5 = 0; num5 < list4.Count; num5++)
			{
				if (list4[num5] is PawnCapacityUtility.CapacityImpactorCapacity)
				{
					stringBuilder.AppendLine("  " + list4[num5].Readable(pawn));
				}
			}
			for (int num6 = 0; num6 < list4.Count; num6++)
			{
				if (list4[num6] is PawnCapacityUtility.CapacityImpactorPain)
				{
					stringBuilder.AppendLine("  " + list4[num6].Readable(pawn));
				}
			}
		}
		return stringBuilder.ToString();
	}

	public static float CalcGearAffects(StatBar bar, Pawn p, ref List<(Thing, float)> list)
	{
		float num = 0f;
		List<Apparel> list2 = p.apparel?.WornApparel;
		if (list2 != null)
		{
			foreach (Apparel item in list2)
			{
				float statOffsetFromList = item.def.equippedStatOffsets.GetStatOffsetFromList(StatDefOf.MoveSpeed);
				num += statOffsetFromList;
				if (statOffsetFromList != 0f)
				{
					list.Add((item, statOffsetFromList));
				}
			}
		}
		List<ThingWithComps> list3 = p.equipment?.AllEquipmentListForReading;
		if (list3 != null)
		{
			foreach (ThingWithComps item2 in list3)
			{
				float statOffsetFromList2 = item2.def.equippedStatOffsets.GetStatOffsetFromList(StatDefOf.MoveSpeed);
				num += statOffsetFromList2;
				if (statOffsetFromList2 != 0f)
				{
					list.Add((item2, statOffsetFromList2));
				}
			}
		}
		return num;
	}

	public static float MaxMoveSpeed(Pawn p)
	{
		return 8.5f;
	}
}
