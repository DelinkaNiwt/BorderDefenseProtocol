using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace NiceInventoryTab;

public static class ArmorUtility
{
	public class ArmorFormat
	{
		public bool IsPercent = true;

		public string Format = string.Empty;

		public float ItemMax = 1.2f;

		public float PawnMax = 1.8f;

		public ArmorFormat(bool isPercent, string format, float itemMax, float pawnMax)
		{
			IsPercent = isPercent;
			Format = format;
			ItemMax = itemMax;
			PawnMax = pawnMax;
		}

		public void SetFor(StatDrawer armorStat)
		{
			if (IsPercent)
			{
				armorStat.SetFormatMode(StatDrawer.FormatMode.Percent);
			}
			else
			{
				armorStat.SetFormat(Format);
			}
		}

		public string Solve(float value)
		{
			if (IsPercent)
			{
				return value.ToStringPercent();
			}
			return value.ToString(Format);
		}
	}

	public static ArmorFormat SharpFormat = new ArmorFormat(isPercent: true, null, 1.2f, 1.8f);

	public static ArmorFormat BluntFormat = new ArmorFormat(isPercent: true, null, 1.2f, 1f);

	public static ArmorFormat HeatFormat = new ArmorFormat(isPercent: true, null, 1.2f, 1.4f);

	public static float SharpArmor(Pawn pawn, StatDrawer statBar)
	{
		float armorWithOverlays = GetArmorWithOverlays(pawn, statBar, StatDefOf.ArmorRating_Sharp, 0);
		if (statBar != null)
		{
			StatRequest req = StatRequest.For(pawn);
			statBar.Descr = StatDefOf.ArmorRating_Sharp.LabelCap + "\n\n" + StatDefOf.ArmorRating_Sharp.Worker.GetExplanationFull(req, StatDefOf.ArmorRating_Sharp.toStringNumberSense, armorWithOverlays);
			ExtraDamageResistInfo(pawn, statBar);
		}
		return armorWithOverlays;
	}

	public static float BluntArmor(Pawn pawn, StatDrawer statBar)
	{
		float armorWithOverlays = GetArmorWithOverlays(pawn, statBar, StatDefOf.ArmorRating_Blunt, 0);
		if (statBar != null)
		{
			StatRequest req = StatRequest.For(pawn);
			statBar.Descr = StatDefOf.ArmorRating_Blunt.LabelCap + "\n\n" + StatDefOf.ArmorRating_Blunt.Worker.GetExplanationFull(req, StatDefOf.ArmorRating_Blunt.toStringNumberSense, armorWithOverlays);
			ExtraDamageResistInfo(pawn, statBar);
		}
		return armorWithOverlays;
	}

	public static void ExtraDamageResistInfo(Pawn pawn, StatDrawer statBar)
	{
		statBar.Descr += "\n\n";
		statBar.Descr += StatDefOf.MeleeDodgeChance.LabelCap + ": " + pawn.GetStatValue(StatDefOf.MeleeDodgeChance).ToStringPercent();
		statBar.Descr += "\n";
		(statBar as StatBar).Overbuffed = false;
		float statValue = pawn.GetStatValue(StatDefOf.IncomingDamageFactor);
		if (Mathf.Abs(statValue - 1f) <= 0.025f)
		{
			statBar.Descr += StatDefOf.IncomingDamageFactor.LabelCap + ": " + statValue.ToStringPercent();
			return;
		}
		if (pawn.IsColonist)
		{
			statBar.Descr += StatDefOf.IncomingDamageFactor.LabelCap + ": " + statValue.ToStringPercent().Colorize(Assets.GoodOrBad(statValue < 1f));
			return;
		}
		(statBar as StatBar).Overbuffed = true;
		statBar.Descr += StatDefOf.IncomingDamageFactor.LabelCap + ": " + statValue.ToStringPercent().Colorize(Assets.PenaltyColor);
	}

	public static float HeatArmor(Pawn pawn, StatDrawer statBar)
	{
		float armorWithOverlays = GetArmorWithOverlays(pawn, statBar, StatDefOf.ArmorRating_Heat, 0);
		if (statBar != null)
		{
			StatRequest req = StatRequest.For(pawn);
			statBar.Descr = StatDefOf.ArmorRating_Heat.LabelCap + "\n\n" + StatDefOf.ArmorRating_Heat.Worker.GetExplanationFull(req, StatDefOf.ArmorRating_Heat.toStringNumberSense, armorWithOverlays);
		}
		return armorWithOverlays;
	}

	public static float MaxSharpArmor(Pawn p)
	{
		return SharpFormat.PawnMax;
	}

	public static float MaxBluntArmor(Pawn p)
	{
		return BluntFormat.PawnMax;
	}

	public static float MaxHeatArmor(Pawn p)
	{
		return HeatFormat.PawnMax;
	}

	public static float MaxPercent(Pawn p)
	{
		return 1f;
	}

	public static float ToxicResist(Pawn pawn, StatDrawer statBar)
	{
		return CommonStatUtility.SolveStat(pawn, statBar, StatDefOf.ToxicEnvironmentResistance);
	}

	public static float VacuumResist(Pawn pawn, StatDrawer drawer)
	{
		if (ModsConfig.OdysseyActive)
		{
			return CommonStatUtility.SolveStat(pawn, drawer, StatDefOf.VacuumResistance, medCheck: false);
		}
		return 0f;
	}

	public static float Flamability(Pawn pawn, StatDrawer drawer)
	{
		return CommonStatUtility.SolveStat(pawn, drawer, StatDefOf.Flammability, medCheck: false);
	}

	public static float PsychicSensitivity(Pawn pawn, StatDrawer drawer)
	{
		return CommonStatUtility.SolveStat(pawn, drawer, StatDefOf.PsychicSensitivity);
	}

	public static float GetArmorWithOverlays(Pawn p, StatDrawer statBar, StatDef stat, int colorID)
	{
		if (!Settings.NoArmorCap)
		{
			return GetArmor(p, stat);
		}
		return GetArmorNoCap(p, stat);
	}

	private static float GetArmor(Pawn pawn, StatDef stat)
	{
		if (pawn?.RaceProps?.body == null)
		{
			return 0f;
		}
		float num = 0f;
		float num2 = Mathf.Clamp01(pawn.GetStatValue(stat) / 2f);
		List<BodyPartRecord> allParts = pawn.RaceProps.body.AllParts;
		List<Apparel> list = pawn.apparel?.WornApparel;
		foreach (BodyPartRecord item in allParts)
		{
			float num3 = 1f - num2;
			if (list != null)
			{
				foreach (Apparel item2 in list)
				{
					if (item2.def.apparel.CoversBodyPart(item))
					{
						float num4 = Mathf.Clamp01(item2.GetStatValue(stat) / 2f);
						num3 *= 1f - num4;
					}
				}
			}
			float num5 = 1f - num3;
			num += item.coverageAbs * num5;
		}
		return Mathf.Clamp(num * 2f, 0f, 2f);
	}

	private static float GetArmorNoCap(Pawn pawn, StatDef stat)
	{
		if (pawn?.RaceProps?.body == null)
		{
			return 0f;
		}
		float num = 0f;
		float num2 = Mathf.Max(pawn.GetStatValue(stat) / 2f, 0f);
		List<BodyPartRecord> allParts = pawn.RaceProps.body.AllParts;
		List<Apparel> list = pawn.apparel?.WornApparel;
		foreach (BodyPartRecord item in allParts)
		{
			float num3 = 1f - num2;
			if (list != null)
			{
				foreach (Apparel item2 in list)
				{
					if (item2.def.apparel.CoversBodyPart(item))
					{
						float num4 = Mathf.Max(item2.GetStatValue(stat) / 2f, 0f);
						num3 *= 1f - num4;
					}
				}
			}
			float num5 = 1f - num3;
			num += item.coverageAbs * num5;
		}
		return Mathf.Max(num * 2f, 0f);
	}

	public static float MaxPsychicSensitivity(Pawn pawn)
	{
		return 2f;
	}
}
