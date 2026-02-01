using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace VanillaPsycastsExpanded;

public class PsycastsMod : Mod
{
	public static Harmony Harm;

	public static PsycastSettings Settings;

	private static BackCompatibilityConverter_Psytrainers psytrainerConverter;

	public PsycastsMod(ModContentPack content)
		: base(content)
	{
		Harm = new Harmony("OskarPotocki.VanillaPsycastsExpanded");
		Settings = GetSettings<PsycastSettings>();
		Harm.Patch(AccessTools.Method(typeof(ThingDefGenerator_Neurotrainer), "ImpliedThingDefs"), null, new HarmonyMethod(typeof(ThingDefGenerator_Neurotrainer_ImpliedThingDefs_Patch), "Postfix"));
		Harm.Patch(AccessTools.Method(typeof(GenDefDatabase), "GetDef"), new HarmonyMethod(GetType(), "PreGetDef"));
		Harm.Patch(AccessTools.Method(typeof(GenDefDatabase), "GetDefSilentFail"), new HarmonyMethod(GetType(), "PreGetDef"));
		List<BackCompatibilityConverter> obj = (List<BackCompatibilityConverter>)AccessTools.Field(typeof(BackCompatibility), "conversionChain").GetValue(null);
		obj.Add(psytrainerConverter = new BackCompatibilityConverter_Psytrainers());
		obj.Add(new BackCompatibilityConverter_Constructs());
		if (ModsConfig.IsActive("GhostRolly.Rim73") || ModsConfig.IsActive("GhostRolly.Rim73_steam"))
		{
			Log.Warning("Vanilla Psycasts Expanded detected Rim73 mod. The mod is throttling hediff ticking which breaks psycast hediffs. You can turn off Rim73 hediff optimization in its mod settings to ensure proper work of Vanilla Psycasts Expanded.");
		}
		LongEventHandler.ExecuteWhenFinished(ApplySettings);
	}

	public override string SettingsCategory()
	{
		return "VanillaPsycastsExpanded".Translate();
	}

	public override void WriteSettings()
	{
		base.WriteSettings();
		ApplySettings();
	}

	private void ApplySettings()
	{
		HediffDefOf.PsychicAmplifier.maxSeverity = Settings.maxLevel;
	}

	public override void DoSettingsWindowContents(Rect inRect)
	{
		base.DoSettingsWindowContents(inRect);
		Listing_Standard listing_Standard = new Listing_Standard();
		listing_Standard.Begin(inRect);
		listing_Standard.Label(string.Concat("VPE.XPPerPercent".Translate() + ": ", Settings.XPPerPercent.ToString()));
		Settings.XPPerPercent = listing_Standard.Slider(Settings.XPPerPercent, 0f, 10f);
		listing_Standard.Label(string.Concat("VPE.PsycasterSpawnBaseChance".Translate() + ": ", (Settings.baseSpawnChance * 100f).ToString(), "%"));
		Settings.baseSpawnChance = listing_Standard.Slider(Settings.baseSpawnChance, 0f, 1f);
		listing_Standard.Label(string.Concat("VPE.PsycasterSpawnAdditional".Translate() + ": ", (Settings.additionalAbilityChance * 100f).ToString(), "%"));
		Settings.additionalAbilityChance = listing_Standard.Slider(Settings.additionalAbilityChance, 0f, 1f);
		listing_Standard.CheckboxLabeled("VPE.AllowShrink".Translate(), ref Settings.shrink, "VPE.AllowShrink.Desc".Translate());
		listing_Standard.CheckboxMultiLabeled("VPE.SmallMode".Translate(), ref Settings.smallMode, "VPE.SmallMode.Desc".Translate());
		listing_Standard.CheckboxLabeled("VPE.MuteSkipdoor".Translate(), ref Settings.muteSkipdoor);
		listing_Standard.Label(string.Concat("VPE.MaxLevel".Translate() + ": ", Settings.maxLevel.ToString()));
		Settings.maxLevel = (int)listing_Standard.Slider(Settings.maxLevel, 1f, 300f);
		listing_Standard.CheckboxLabeled("VPE.ChangeFocusGain".Translate(), ref Settings.changeFocusGain, "VPE.ChangeFocusGain.Desc".Translate());
		listing_Standard.End();
	}

	public static void PreGetDef(Type __0, ref string __1, bool __2)
	{
		if (__2)
		{
			string text = psytrainerConverter.BackCompatibleDefName(__0, __1);
			if (text != null)
			{
				__1 = text;
			}
		}
	}
}
