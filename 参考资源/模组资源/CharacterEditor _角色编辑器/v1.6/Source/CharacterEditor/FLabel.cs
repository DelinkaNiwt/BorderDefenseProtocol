using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace CharacterEditor;

internal static class FLabel
{
	internal static Func<ScenPart, string> ScenPartTooltip => delegate(ScenPart p)
	{
		Selected selectedScenarioPart = p.GetSelectedScenarioPart();
		if (p.IsScenarioAnimal())
		{
			if (selectedScenarioPart.pkd != null)
			{
				return selectedScenarioPart.pkd.STooltip();
			}
			return p.STooltip();
		}
		return (selectedScenarioPart.thingDef != null) ? selectedScenarioPart.thingDef.STooltip() : "";
	};

	internal static Func<ScenPart, string> ScenPartLabel => delegate(ScenPart p)
	{
		Selected selectedScenarioPart = p.GetSelectedScenarioPart();
		if (p.IsScenarioAnimal())
		{
			if (selectedScenarioPart.pkd != null)
			{
				return PawnKindWithGenderAndAge(selectedScenarioPart);
			}
			return p.Label + ": " + selectedScenarioPart.stackVal;
		}
		return (selectedScenarioPart.thingDef != null) ? ThingLabel(selectedScenarioPart) : "";
	};

	internal static Func<float, string> BodySizeFactor => (float x) => Label.BODYSIZEFACTOR + " " + x;

	internal static Func<float?, string> BodyWidth => (float? x) => Label.BODYWIDTH + " " + x;

	internal static Func<float?, string> HeadSizeFactor => (float? x) => Label.HEADSIZEFACTOR + " " + x;

	internal static Func<float?, string> EyeSizeFactor => (float? x) => Label.EYESIZEFACTOR + " " + x;

	internal static Func<float, string> VoicePitch => (float x) => Label.VOICEPITCH + " " + x;

	internal static Func<float, string> VoiceVolume => (float x) => Label.VOICEVOLUME + " " + x;

	internal static Func<float, string> HealthScaleFactor => (float x) => Label.HEALTHSCALEFACTOR + " " + x;

	internal static Func<float, string> HungerRateFactor => (float x) => Label.HUNGERRATEFACTOR + " " + x;

	internal static Func<float, string> MarketValueFactor => (float x) => Label.MARKETFACTOR + " " + x;

	internal static Func<float, string> FoodMaxFactor => (float x) => Label.FOODMAXFACTOR + " " + x;

	internal static Func<float, string> MeleeDamageFactor => (float x) => Label.MELEEDAMAGEFACTOR + " " + x;

	internal static Func<Selected, string> PawnKindWithGenderAndAge => (Selected s) => PawnKindDefLabel(s.pkd) + " x" + s.stackVal + " " + GenderLabel(s.gender) + "[" + s.age + "]";

	internal static Func<Selected, string> ThingLabel => (Selected s) => GenLabel.ThingLabel(s.thingDef, s.stuff, s.stackVal).CapitalizeFirst() + (s.HasQuality ? (" (" + ((QualityCategory)s.quality).GetLabel() + ")") : "");

	internal static Func<int, string> GenderLabelInt => (int x) => GenderLabel((Gender)x);

	internal static Func<int, string> BiologicalAge => (int x) => Label.BIOAGE + ": " + x;

	internal static Func<Gender, string> GenderLabel => (Gender x) => x switch
	{
		Gender.Male => "Male".Translate().ToString(), 
		Gender.Female => "Female".Translate().ToString(), 
		_ => Label.RANDOMCHOSEN, 
	};

	internal static Func<PawnKindDef, string> PawnKindDefLabel => (PawnKindDef x) => (x.race != null) ? x.race.label : "";

	internal static Func<WeaponType, string> WeaponType => (WeaponType type) => WeaponTool.GetNameForWeaponType(type);

	internal static Func<int, string> Menge => (int x) => Label.COUNT + x;

	internal static Func<int, string> HitPoints => (int x) => "HitPoints".Translate(x);

	internal static Func<int, string> DamageAmountBase => (int x) => Label.DAMAGEAMOUNTBASE + GetFormattedValue("", x);

	internal static Func<int, string> CostStuffCount => (int x) => Label.COSTSTUFFCOUNT + " " + x;

	internal static Func<int, string> StackLimit => (int x) => Label.O_STACKLIMIT + " " + x;

	internal static Func<GeneticBodyType?, string> GeneticBodytype => (GeneticBodyType? x) => (!x.HasValue) ? "" : Enum.GetName(typeof(GeneticBodyType), x);

	internal static Func<EndogeneCategory, string> EndogeneCat => (EndogeneCategory x) => Enum.GetName(typeof(EndogeneCategory), x);

	internal static Func<Tradeability, string> Tradeability => (Tradeability x) => Enum.GetName(typeof(Tradeability), x);

	internal static Func<TechLevel, string> TechLevel => (TechLevel x) => Enum.GetName(typeof(TechLevel), x);

	internal static Func<GasType?, string> GasType => (GasType? x) => (!x.HasValue) ? "" : Enum.GetName(typeof(GasType), x);

	internal static Func<SkillDef, string> PassionModAdd => (SkillDef x) => (x == null) ? Label.NONE : "PassionModAdd".Translate(x).ToString();

	internal static Func<SkillDef, string> PassionModDrop => (SkillDef x) => (x == null) ? Label.NONE : "PassionModDrop".Translate(x).ToString();

	internal static Func<SoundDef, string> Sound => (SoundDef x) => x.SDefname();

	internal static Func<float, string> BeamWidth => (float x) => Label.BEAMSTREUUNG + GetFormattedValue("cells", x);

	internal static Func<float, string> BeamFullWidthRange => (float x) => Label.BEAMFULLWIDTHRANGE + GetFormattedValue("cells", x);

	internal static Func<float, string> CooldownTime => (float x) => "CooldownTime".Translate() + GetFormattedValue("s", x);

	internal static Func<float, string> AccuracyLong => (float x) => "Base".Translate() + " " + StatDefOf.AccuracyLong.label + GetFormattedValue("%", x);

	internal static Func<float, string> AccuracyMedium => (float x) => "Base".Translate() + " " + StatDefOf.AccuracyMedium.label + GetFormattedValue("%", x);

	internal static Func<float, string> AccuracyShort => (float x) => "Base".Translate() + " " + StatDefOf.AccuracyShort.label + GetFormattedValue("%", x);

	internal static Func<float, string> AccuracyTouch => (float x) => "Base".Translate() + " " + StatDefOf.AccuracyTouch.label + GetFormattedValue("%", x);

	internal static Func<float, string> Spraying => (float x) => Label.SPRAYING + GetFormattedValue("cells", x);

	internal static Func<float, string> SprayingMortar => (float x) => Label.SPRAYING + Label.CLASSICMORTAR + GetFormattedValue("cells", x);

	internal static Func<float, string> ConsumeFuelPerBurst => (float x) => Label.CONSUMEFUELPERBURST + GetFormattedValue("", x);

	internal static Func<float, string> ConsumeFuelPerShot => (float x) => Label.CONSUMEFUELPERSHOT + GetFormattedValue("", x);

	internal static Func<float, string> BulletPostExplosionSpawnChance => (float x) => Label.POSTEXPLOSIONSPAWNCHANCE + GetFormattedValue("%", x);

	internal static Func<float, string> BulletExplosionSpawnChance => (float x) => Label.PREEXPLOSIONSPAWNCHANCE + GetFormattedValue("%", x);

	internal static Func<int, string> BulletPostExplosionSpawnThingCount => (int x) => Label.POSTEXPLOSIONSPAWNTHINGCOUNT + GetFormattedValue("", x);

	internal static Func<int, string> BulletExplosionSpawnThingCount => (int x) => Label.PREEXPLOSIONSPAWNTHINGCOUNT + GetFormattedValue("", x);

	internal static Func<int, string> BulletExplosionDelay => (int x) => Label.EXPLOSIONDELAY + GetFormattedValue("s", x);

	internal static Func<int, string> BulletNumExtraHitCels => (int x) => Label.NUMEXTRAHITCELLS + GetFormattedValue("cells", x);

	internal static Func<float, string> BulletExplosionRadius => (float x) => Label.EXPL_RADIUS + GetFormattedValue("cells", x);

	internal static Func<float, string> ArmorPenetrationBase => (float x) => "ArmorPenetration".Translate() + GetFormattedValue("", x);

	internal static Func<int, string> Complexity => (int x) => Label.COMPLEXITY.Colorize(GeneUtility.GCXColor) + " " + x.ToStringWithSign();

	internal static Func<int, string> Metabolism => (int x) => Label.METABOLICEFFICIENCY.Colorize(GeneUtility.METColor) + " " + x.ToStringWithSign();

	internal static Func<int, string> ArchitesRequired => (int x) => Label.ARCHITECAPSULES.Colorize(GeneUtility.ARCColor) + " " + x.ToStringWithSign();

	internal static Func<float, string> LovinMTBFactor => (float x) => Label.LOVINMTBFACTOR + " " + x;

	internal static Func<float, string> MinAgeActive => (float x) => Label.STARTSATAGE + " " + x;

	internal static Func<float, string> DisplayOrderInCat => (float x) => Label.DISPLAYODERINCATEGORY + " " + x;

	internal static Func<float, string> SelectionWeightDark => (float x) => Label.SELECTIONWEIGHTDARKSKIN + " " + x;

	internal static Func<float, string> SelectionWeight => (float x) => Label.SELECTIONWEIGHT + " " + x;

	internal static Func<float, string> RandomBrightnessFactor => (float x) => Label.RANDOMBRIGHTNESSFACTOR + " " + x;

	internal static Func<float, string> MarketValue => (float x) => Label.MARKETVALUEFACTOR + " " + x;

	internal static Func<float, string> PrisonBreak => (float x) => ((double)x < 0.0) ? "WillNeverPrisonBreak".Translate().ToString() : (Label.PRISONBREAKINTERVAL + " x" + x.ToStringPercent());

	internal static Func<float, string> FoodPoisonChance => (float x) => ((double)x == 1.0) ? "Stat_Hediff_FoodPoisoningChanceFactor_Name".Translate() : (((double)x <= 0.0) ? "FoodPoisoningImmune".Translate() : ("Stat_Hediff_FoodPoisoningChanceFactor_Name".Translate() + " x" + x.ToStringPercent()));

	internal static Func<float, string> PainOffset => (float x) => "Pain".Translate() + " " + (x * 100f).ToString("+###0;-###0") + "%";

	internal static Func<float, string> PainFactor => (float x) => "Pain".Translate() + " x" + x.ToStringPercent();

	internal static Func<float, string> AddictionChance => delegate(float x)
	{
		if (GeneTool.SelectedGene.chemical == null)
		{
			return Label.ADDICTIONCHANCEFACTOR;
		}
		return ((double)x <= 0.0) ? ((string)"AddictionImmune".Translate(GeneTool.SelectedGene.chemical)) : ((string)("AddictionChanceFactor".Translate(GeneTool.SelectedGene.chemical) + " x" + x.ToStringPercent()));
	};

	internal static Func<float, string> OverdoseChance => (float x) => (GeneTool.SelectedGene.chemical == null) ? Label.OVERDOSECHANCEFACTOR : ((string)("OverdoseChanceFactor".Translate(GeneTool.SelectedGene.chemical) + " x" + x.ToStringPercent()));

	internal static Func<float, string> ToleranceFactor => (float x) => (GeneTool.SelectedGene.chemical == null) ? Label.TOLERANCEBUILDUPFACTOR : ((string)("ToleranceBuildupFactor".Translate(GeneTool.SelectedGene.chemical) + " x" + x.ToStringPercent()));

	internal static Func<float, string> ResourceLoss => (float x) => GeneTool.SelectedGene.resourceLabel.NullOrEmpty() ? Label.RESOURCELOSSPERDAY : "ResourceLossPerDay".Translate(GeneTool.SelectedGene.resourceLabel.Named("RESOURCE"), (-Mathf.RoundToInt(GeneTool.SelectedGene.resourceLossPerDay * 100f)).ToStringWithSign().Named("OFFSET")).ToString();

	internal static Func<float, string> MissingRomanceChance => (float x) => ((double)x != 1.0) ? ("MissingGeneRomanceChance".Translate(GeneTool.SelectedGene.label.Named("GENE")) + " x" + x.ToStringPercent()) : "MissingGeneRomanceChance".Translate();

	internal static Func<float, string> MentalBreakMTB => (float x) => ((double)x == 0.0) ? Label.MENTALBREAKMTBDAYS : (Label.MENTALBREAKMTBDAYS + " " + x);

	internal static Func<float, string> MentalBreakChance => (float x) => ((double)x == 1.0) ? Label.MENTALBREAKCHANCEFACTOR : (((double)x <= 0.0) ? "NeverAggroMentalBreak".Translate().ToString() : ("AggroMentalBreakSelectionChanceFactor".Translate().ToString() + " x" + x.ToStringPercent()));

	internal static Func<float, string> SocialFightChance => (float x) => ((double)x <= 0.0) ? "WillNeverSocialFight".Translate().ToString() : (Label.SOCIALFIGHTCHANCEFACTOR + " x" + x.ToStringPercent());

	internal static string DefDescription<T>(T def) where T : Def
	{
		return def.STooltip();
	}

	internal static string DefLabel<T>(T def) where T : Def
	{
		return "[" + def.SLabel() + "]";
	}

	internal static string DefLabelSimple<T>(T def) where T : Def
	{
		string text = def.SLabel();
		if (text == Label.NONE)
		{
			return Label.ALL;
		}
		return text;
	}

	internal static string DefName<T>(T def) where T : Def
	{
		return "[" + def.SDefname() + "]";
	}

	internal static string TString(string t)
	{
		return t.NullOrEmpty() ? Label.ALL : t;
	}

	internal static string EnumNameAndAll<T>(T e)
	{
		return (e.ToString() == "None") ? Label.ALL : e.ToString();
	}

	internal static string GetFormattedValue(string format, float value)
	{
		switch (format)
		{
		case "%":
			return " [" + Math.Round(100f * value, 0) + " %]";
		case "s":
			return " [" + value + " s]";
		case "ticks":
			return " [" + value + " ticks]";
		case "rpm":
			return " [" + ((value == 0f) ? Label.INFINITE : Math.Round(60f / value * 60f, 0).ToString()) + " rpm]";
		case "cps":
			return " [" + ((value == 0f) ? Label.INFINITE : value.ToString()) + " cps]";
		case "cells":
			return " [" + value + " cells]";
		default:
			if (format.StartsWith("max"))
			{
				return " [" + value + "/" + format.SubstringFrom("max") + "]";
			}
			switch (format)
			{
			case "int":
				return " [" + (int)Math.Round(value) + "]";
			case "quadrum":
				return " [" + Enum.GetName(typeof(Quadrum), (int)value) + "]";
			case "addict":
				return " " + (100.0 - Math.Round(100f * value, 0)) + " %";
			case "high":
				return " " + value.ToStringPercent("F0");
			default:
				if (format.StartsWith("DEF"))
				{
					return " [" + format.SubstringFrom("DEF") + "]";
				}
				if (format.StartsWith("dauer"))
				{
					return " " + format.SubstringFrom("dauer");
				}
				if (format == "pain")
				{
					int num = (int)value;
					return (num == 0) ? Label.PAINLESS : ("PainCategory_" + HealthTool.ConvertSliderToPainCategory(num)).Translate().ToString();
				}
				if (format.StartsWith("comp"))
				{
					if (!format.Contains("%"))
					{
						return " " + value.ToStringPercent("F0") + " " + format.SubstringFrom("comp");
					}
					return " " + format.SubstringFrom("comp");
				}
				return " [" + value + "]";
			}
		}
	}
}
