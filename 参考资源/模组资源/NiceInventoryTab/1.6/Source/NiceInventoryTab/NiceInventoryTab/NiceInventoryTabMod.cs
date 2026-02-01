using HarmonyLib;
using UnityEngine;
using Verse;

namespace NiceInventoryTab;

public class NiceInventoryTabMod : Mod
{
	public static NiceInventoryTabMod instance;

	public static Harmony harmonyInstance;

	public static Settings settings;

	public NiceInventoryTabMod(ModContentPack content)
		: base(content)
	{
		instance = this;
		settings = GetSettings<Settings>();
		harmonyInstance = new Harmony("Andromeda.NiceInventoryTab");
		harmonyInstance.PatchAll();
		ModIntegration.DoPatches(harmonyInstance);
	}

	public override string SettingsCategory()
	{
		return "Nice Inventory Tab";
	}

	public override void DoSettingsWindowContents(Rect inRect)
	{
		settings.DoWindowContents(inRect);
	}
}
