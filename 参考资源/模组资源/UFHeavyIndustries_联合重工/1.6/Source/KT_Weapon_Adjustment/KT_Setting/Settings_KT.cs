using System.Collections.Generic;
using Verse;

namespace KT_Setting;

public class Settings_KT : ModSettings
{
	public float Weapon_Patch4_KT;

	public float Weapon_Patch5_KT = 1f;

	public float Weapon_Patch6_KT = 1f;

	public float Weapon_Patch7_KT = 1f;

	public float Weapon_Patch8_KT = 1f;

	public float Weapon_Patch9_KT = 1f;

	public Dictionary<string, WeaponAndAmount> dictionary_Weapon_Damage_KT = new Dictionary<string, WeaponAndAmount>();

	public Dictionary<string, WeaponAndCooldown> dictionary_Weapon_Cooldown_KT = new Dictionary<string, WeaponAndCooldown>();

	public Dictionary<string, DamageAndAmount> dictionary_DamageDef_Damage_KT = new Dictionary<string, DamageAndAmount>();

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref Weapon_Patch5_KT, "Weapon_Patch5_KT2", 1f);
		Scribe_Values.Look(ref Weapon_Patch6_KT, "Weapon_Patch6_KT2", 1f);
		Scribe_Values.Look(ref Weapon_Patch7_KT, "Weapon_Patch7_KT2", 1f);
		Scribe_Values.Look(ref Weapon_Patch8_KT, "Weapon_Patch8_KT2", 1f);
		Scribe_Values.Look(ref Weapon_Patch9_KT, "Weapon_Patch9_KT2", 1f);
		InitData();
	}

	public void InitData()
	{
		Scribe_Values.Look(ref Weapon_Patch5_KT, "Weapon_Patch5_KT2", 1f);
		Scribe_Values.Look(ref Weapon_Patch6_KT, "Weapon_Patch6_KT2", 1f);
		Scribe_Values.Look(ref Weapon_Patch7_KT, "Weapon_Patch7_KT2", 1f);
		Scribe_Values.Look(ref Weapon_Patch8_KT, "Weapon_Patch8_KT2", 1f);
		Scribe_Values.Look(ref Weapon_Patch9_KT, "Weapon_Patch9_KT2", 1f);
	}
}
