using UnityEngine;
using Verse;

namespace TOT_DLL_test;

[StaticConstructorOnStartup]
public class Settings_CMC_Main : Mod
{
	private int ticks = 0;

	public Settings_CMC settings_CMC;

	public int Damage = -1;

	public static Settings_CMC_Main Instance { get; private set; }

	public Settings_CMC_Main(ModContentPack content)
		: base(content)
	{
		settings_CMC = GetSettings<Settings_CMC>();
		Instance = this;
	}

	public override string SettingsCategory()
	{
		return "CeleTech Arsenal MKIII";
	}

	public override void DoSettingsWindowContents(Rect inRect)
	{
		ticks++;
		Listing_Standard listing_Standard = new Listing_Standard();
		listing_Standard.Begin(new Rect(inRect.x, inRect.y, inRect.width, inRect.height));
		listing_Standard.GapLine(20f);
		listing_Standard.Label("Setting_CMC".Translate());
		listing_Standard.CheckboxLabeled("Setting1_CMC".Translate(), ref settings_CMC.Weapon_Patch1_CMC, "Setting_D1_CMC".Translate());
		listing_Standard.CheckboxLabeled("Setting3_CMC".Translate(), ref settings_CMC.Weapon_Patch3_CMC, "Setting_D3_CMC".Translate());
		listing_Standard.GapLine(10f);
		listing_Standard.Label("Setting_label1_CMC".Translate());
		settings_CMC.Weapon_Patch5_CMC = listing_Standard.SliderLabeled(string.Concat("Setting5_CMC".Translate() + ": ", ((float)(int)((double)settings_CMC.Weapon_Patch5_CMC / 0.1) * 0.1f).ToString()), (float)(int)((double)settings_CMC.Weapon_Patch5_CMC / 0.1) * 0.1f, 0.3f, 6f, 0.3f, "Setting5_1_CMC".Translate());
		settings_CMC.Weapon_Patch6_CMC = listing_Standard.SliderLabeled(string.Concat("Setting6_CMC".Translate() + ": ", ((float)(int)((double)settings_CMC.Weapon_Patch6_CMC / 0.1) * 0.1f).ToString()), (float)(int)((double)settings_CMC.Weapon_Patch6_CMC / 0.1) * 0.1f, 0.3f, 6f, 0.3f, "Setting6_1_CMC".Translate());
		settings_CMC.Weapon_Patch7_CMC = listing_Standard.SliderLabeled(string.Concat("Setting7_CMC".Translate() + ": ", ((float)(int)((double)settings_CMC.Weapon_Patch7_CMC / 0.1) * 0.1f).ToString()), (float)(int)((double)settings_CMC.Weapon_Patch7_CMC / 0.1) * 0.1f, 0.3f, 6f, 0.3f, "Setting7_1_CMC".Translate());
		settings_CMC.Weapon_Patch9_CMC = listing_Standard.SliderLabeled(string.Concat("Setting9_CMC".Translate() + ": ", ((float)(int)((double)settings_CMC.Weapon_Patch9_CMC / 0.1) * 0.1f).ToString()), (float)(int)((double)settings_CMC.Weapon_Patch9_CMC / 0.1) * 0.1f, 0.1f, 2f, 0.3f, "Setting9_1_CMC".Translate());
		settings_CMC.Weapon_Patch8_CMC = listing_Standard.SliderLabeled(string.Concat("Setting8_CMC".Translate() + ": ", ((float)(int)((double)settings_CMC.Weapon_Patch8_CMC / 0.1) * 0.1f).ToString()), (float)(int)((double)settings_CMC.Weapon_Patch8_CMC / 0.1) * 0.1f, 0.1f, 2f, 0.3f, "Setting8_1_CMC".Translate());
		listing_Standard.End();
		if (ticks > 100)
		{
			SimpleClass.Ado.Doing();
			ticks = 0;
		}
	}
}
