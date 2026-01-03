using System.Collections.Generic;
using Verse;

namespace TOT_DLL_test;

public class Settings_CMC : ModSettings
{
	public bool Weapon_Patch1_CMC;

	public bool Weapon_Patch2_CMC;

	public bool Weapon_Patch3_CMC;

	public float Weapon_Patch4_CMC;

	public float Weapon_Patch5_CMC = 1f;

	public float Weapon_Patch6_CMC = 1f;

	public float Weapon_Patch7_CMC = 1f;

	public float Weapon_Patch8_CMC = 1f;

	public float Weapon_Patch9_CMC = 1f;

	public Dictionary<string, WeaponAndAmount> dictionary_Weapon_Damage_CMC = new Dictionary<string, WeaponAndAmount>();

	public Dictionary<string, WeaponAndCooldown> dictionary_Weapon_Cooldown_CMC = new Dictionary<string, WeaponAndCooldown>();

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref Weapon_Patch1_CMC, "Weapon_Patch_CMC", defaultValue: false);
		Scribe_Values.Look(ref Weapon_Patch2_CMC, "Weapon_Patch2_CMC", defaultValue: false);
		Scribe_Values.Look(ref Weapon_Patch3_CMC, "Weapon_Patch3_CMC", defaultValue: true);
		Scribe_Values.Look(ref Weapon_Patch5_CMC, "Weapon_Patch5_CMC", 1f);
		Scribe_Values.Look(ref Weapon_Patch6_CMC, "Weapon_Patch6_CMC", 1f);
		Scribe_Values.Look(ref Weapon_Patch7_CMC, "Weapon_Patch7_CMC", 1f);
		Scribe_Values.Look(ref Weapon_Patch8_CMC, "Weapon_Patch8_CMC", 1f);
		Scribe_Values.Look(ref Weapon_Patch9_CMC, "Weapon_Patch9_CMC", 1f);
		InitData();
	}

	public void InitData()
	{
		Scribe_Values.Look(ref Weapon_Patch1_CMC, "Weapon_Patch_CMC", defaultValue: false);
		Scribe_Values.Look(ref Weapon_Patch2_CMC, "Weapon_Patch2_CMC", defaultValue: false);
		Scribe_Values.Look(ref Weapon_Patch3_CMC, "Weapon_Patch3_CMC", defaultValue: true);
		Scribe_Values.Look(ref Weapon_Patch5_CMC, "Weapon_Patch5_CMC", 1f);
		Scribe_Values.Look(ref Weapon_Patch6_CMC, "Weapon_Patch6_CMC", 1f);
		Scribe_Values.Look(ref Weapon_Patch7_CMC, "Weapon_Patch7_CMC", 1f);
		Scribe_Values.Look(ref Weapon_Patch8_CMC, "Weapon_Patch8_CMC", 1f);
		Scribe_Values.Look(ref Weapon_Patch9_CMC, "Weapon_Patch9_CMC", 1f);
	}
}
