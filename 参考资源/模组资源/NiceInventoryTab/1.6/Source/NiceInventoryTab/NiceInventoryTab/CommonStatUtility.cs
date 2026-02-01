using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using Verse;

namespace NiceInventoryTab;

internal class CommonStatUtility
{
	public static MethodInfo ActiveForMI = typeof(StatPart_Glow).GetMethod("ActiveFor", BindingFlags.Instance | BindingFlags.NonPublic);

	public static MethodInfo FactorFromGlowMI = typeof(StatPart_Glow).GetMethod("FactorFromGlow", BindingFlags.Instance | BindingFlags.NonPublic);

	internal static float MaxNegoriation(Pawn pawn)
	{
		return 1.5f;
	}

	internal static float MaxMeditationFocusGain(Pawn pawn)
	{
		return 1.5f;
	}

	internal static float MaxSocialImpact(Pawn pawn)
	{
		return 2f;
	}

	internal static float MaxTendQuality(Pawn pawn)
	{
		return 2f;
	}

	public static float SolveStat(Pawn pawn, StatDrawer drawer, StatDef stat, bool medCheck = true, bool checkLight = false)
	{
		if (stat.Worker.IsDisabledFor(pawn) || pawn.Dead)
		{
			return 0f;
		}
		if (!Settings.DrugImpactVisible || !medCheck || drawer == null)
		{
			float statValue = pawn.GetStatValue(stat);
			if (drawer != null)
			{
				drawer.Descr = MakeToolTip(pawn, stat, statValue);
			}
			return statValue;
		}
		var (num, num2) = HediffUtility.GetStatValueFilterHediffs(pawn, stat, HediffUtility.IsPermanent_or_IsImplant);
		if (checkLight)
		{
			List<StatDef> statFactors = stat.statFactors;
			if ((statFactors != null && statFactors.Contains(StatDefOf.WorkSpeedGlobal)) || stat == StatDefOf.WorkSpeedGlobal)
			{
				StatPart_Glow statPart = StatDefOf.WorkSpeedGlobal.GetStatPart<StatPart_Glow>();
				if ((bool)ActiveForMI.Invoke(statPart, new object[1] { pawn }))
				{
					float num3 = (float)FactorFromGlowMI.Invoke(statPart, new object[1] { pawn });
					float v = num * (1f - 1f / num3);
					(drawer as StatBar).AddDebuff(v, Assets.EnviromentPenaltyColor);
				}
			}
		}
		float v2 = num - num2;
		(drawer as StatBar).AddAutoBuffDebuff(v2, (drawer as StatBar).ColorBar);
		drawer.Descr = MakeToolTip(pawn, stat, num);
		return num;
	}

	private static string MakeToolTip(Pawn pawn, StatDef stat, float value)
	{
		StatRequest req = StatRequest.For(pawn);
		return stat.LabelCap + "\n\n" + stat.Worker.GetExplanationFull(req, stat.toStringNumberSense, value);
	}

	internal static float NegotiationFactor(Pawn pawn, StatDrawer drawer)
	{
		float result = SolveStat(pawn, drawer, StatDefOf.NegotiationAbility);
		if (drawer != null && !StatDefOf.TradePriceImprovement.Worker.IsDisabledFor(pawn))
		{
			drawer.Descr += "\n\n";
			drawer.Descr += StatDefOf.TradePriceImprovement.LabelCap + ": " + pawn.GetStatValue(StatDefOf.TradePriceImprovement).ToStringPercent();
		}
		return result;
	}

	internal static float MeditationFocusGain(Pawn pawn, StatDrawer drawer)
	{
		if (!ModsConfig.RoyaltyActive)
		{
			return 0f;
		}
		return SolveStat(pawn, drawer, StatDefOf.MeditationFocusGain);
	}

	internal static float SocialImpact(Pawn pawn, StatDrawer drawer)
	{
		float result = SolveStat(pawn, drawer, StatDefOf.SocialImpact);
		if (drawer != null)
		{
			drawer.Descr += "\n\n";
			drawer.Descr += StatDefOf.Beauty.LabelCap + ": " + pawn.GetStatValue(StatDefOf.Beauty).ToString("F1");
			if (!StatDefOf.TameAnimalChance.Worker.IsDisabledFor(pawn))
			{
				drawer.Descr += "\n";
				drawer.Descr += StatDefOf.TameAnimalChance.LabelCap + ": " + pawn.GetStatValue(StatDefOf.TameAnimalChance).ToStringPercent();
			}
		}
		return result;
	}

	internal static float TendQuality(Pawn pawn, StatDrawer drawer)
	{
		float result = SolveStat(pawn, drawer, StatDefOf.MedicalTendQuality);
		if (drawer != null && !StatDefOf.MedicalSurgerySuccessChance.Worker.IsDisabledFor(pawn))
		{
			drawer.Descr += "\n\n";
			drawer.Descr += StatDefOf.MedicalSurgerySuccessChance.LabelCap + ": " + pawn.GetStatValue(StatDefOf.MedicalSurgerySuccessChance).ToStringPercent();
		}
		return result;
	}
}
