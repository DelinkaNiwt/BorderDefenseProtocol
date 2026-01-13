using UnityEngine;
using Verse;

namespace KT_Setting;

[StaticConstructorOnStartup]
public class Settings_Main : Mod
{
	public Settings_KT Settings_KT;

	public int Damage = -1;

	public static Settings_Main Instance { get; private set; }

	public Settings_Main(ModContentPack content)
		: base(content)
	{
		Settings_KT = GetSettings<Settings_KT>();
		Instance = this;
	}

	public override string SettingsCategory()
	{
		return "UF Heavy Industries";
	}

	public override void DoSettingsWindowContents(Rect inRect)
	{
		Listing_Standard listing_Standard = new Listing_Standard();
		listing_Standard.Begin(new Rect(inRect.x, inRect.y, inRect.width, inRect.height));
		listing_Standard.GapLine(20f);
		listing_Standard.Label("Setting_KT".Translate());
		listing_Standard.GapLine(10f);
		listing_Standard.Label("Setting_label1_KT".Translate());
		Settings_KT.Weapon_Patch5_KT = listing_Standard.SliderLabeled(string.Concat("Setting5_KT".Translate() + ": ", ((float)(int)((double)Settings_KT.Weapon_Patch5_KT / 0.25) * 0.25f).ToString()), (float)(int)((double)Settings_KT.Weapon_Patch5_KT / 0.25) * 0.25f, 0.25f, 5f, 0.3f, "Setting5_1_KT".Translate());
		Settings_KT.Weapon_Patch6_KT = listing_Standard.SliderLabeled(string.Concat("Setting6_KT".Translate() + ": ", ((float)(int)((double)Settings_KT.Weapon_Patch6_KT / 0.1) * 0.1f).ToString()), (float)(int)((double)Settings_KT.Weapon_Patch6_KT / 0.1) * 0.1f, 0.3f, 3f, 0.3f, "Setting6_1_KT".Translate());
		Settings_KT.Weapon_Patch7_KT = listing_Standard.SliderLabeled(string.Concat("Setting7_KT".Translate() + ": ", ((float)(int)((double)Settings_KT.Weapon_Patch7_KT / 0.1) * 0.1f).ToString()), (float)(int)((double)Settings_KT.Weapon_Patch7_KT / 0.1) * 0.1f, 0.3f, 3f, 0.3f, "Setting7_1_KT".Translate());
		Settings_KT.Weapon_Patch9_KT = listing_Standard.SliderLabeled(string.Concat("Setting9_KT".Translate() + ": ", ((float)(int)((double)Settings_KT.Weapon_Patch9_KT / 0.1) * 0.1f).ToString()), (float)(int)((double)Settings_KT.Weapon_Patch9_KT / 0.1) * 0.1f, 0.1f, 2f, 0.3f, "Setting9_1_KT".Translate());
		Settings_KT.Weapon_Patch8_KT = listing_Standard.SliderLabeled(string.Concat("Setting8_KT".Translate() + ": ", ((float)(int)((double)Settings_KT.Weapon_Patch8_KT / 0.1) * 0.1f).ToString()), (float)(int)((double)Settings_KT.Weapon_Patch8_KT / 0.1) * 0.1f, 0.1f, 2f, 0.3f, "Setting8_1_KT".Translate());
		listing_Standard.End();
		SimpleClass.Ado.Doing();
	}
}
