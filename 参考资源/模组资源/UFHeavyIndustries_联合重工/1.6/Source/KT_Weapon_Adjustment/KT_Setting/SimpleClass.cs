using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;

namespace KT_Setting;

[StaticConstructorOnStartup]
internal class SimpleClass
{
	public class Ado
	{
		public static void DoingList()
		{
			Settings_Main.Instance.Settings_KT.dictionary_Weapon_Damage_KT.Clear();
			foreach (DamageDef allDef in DefDatabase<DamageDef>.AllDefs)
			{
				if (allDef.defName.Length > 3 && allDef.defName.Length > 3 && allDef.defName.Substring(0, 3) == "KT_")
				{
					DamageAndAmount damageAndAmount = new DamageAndAmount();
					damageAndAmount.damage = allDef;
					damageAndAmount.amount = allDef.defaultDamage;
					damageAndAmount.armorPenetrationBase = allDef.defaultArmorPenetration;
					Settings_Main.Instance.Settings_KT.dictionary_DamageDef_Damage_KT[allDef.defName] = damageAndAmount;
				}
			}
			foreach (ThingDef allDef2 in DefDatabase<ThingDef>.AllDefs)
			{
				if (allDef2.weaponTags != null && allDef2.weaponTags.Contains("KT_weapon") && allDef2.Verbs != null && !allDef2.Verbs.Exists((VerbProperties x) => x.defaultProjectile == null) && allDef2.Verbs.Any())
				{
					float warmupTime = allDef2.Verbs.First().warmupTime;
					float value = allDef2.statBases.Find((StatModifier x) => x.stat == StatDefOf.RangedWeapon_Cooldown).value;
					WeaponAndCooldown weaponAndCooldown = new WeaponAndCooldown();
					weaponAndCooldown.weaponDef = allDef2;
					weaponAndCooldown.cooldownTime = value;
					weaponAndCooldown.warmupTime = warmupTime;
					Settings_Main.Instance.Settings_KT.dictionary_Weapon_Cooldown_KT[allDef2.defName] = weaponAndCooldown;
					VerbProperties verbProperties = allDef2.Verbs.Find((VerbProperties x) => x.defaultProjectile != null);
					if (verbProperties != null)
					{
						ThingDef defaultProjectile = verbProperties.defaultProjectile;
						int amount = (int)typeof(ProjectileProperties).GetField("damageAmountBase", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(defaultProjectile.projectile);
						float armorPenetrationBase = (float)typeof(ProjectileProperties).GetField("armorPenetrationBase", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(defaultProjectile.projectile);
						WeaponAndAmount weaponAndAmount = new WeaponAndAmount();
						weaponAndAmount.projectile = defaultProjectile;
						weaponAndAmount.amount = amount;
						weaponAndAmount.armorPenetrationBase = armorPenetrationBase;
						weaponAndAmount.explosionRadius = defaultProjectile.projectile.explosionRadius;
						Settings_Main.Instance.Settings_KT.dictionary_Weapon_Damage_KT[verbProperties.defaultProjectile.defName] = weaponAndAmount;
					}
				}
			}
		}

		public static void Doing()
		{
			List<string> list = new List<string>();
			int num = 0;
			foreach (KeyValuePair<string, DamageAndAmount> item in Settings_Main.Instance.Settings_KT.dictionary_DamageDef_Damage_KT)
			{
				DamageDef damage = item.Value.damage;
				if (damage.defaultDamage > 0)
				{
					float weapon_Patch5_KT = Settings_Main.Instance.Settings_KT.Weapon_Patch5_KT;
					damage.defaultDamage = (int)((float)item.Value.amount * weapon_Patch5_KT);
					weapon_Patch5_KT = Settings_Main.Instance.Settings_KT.Weapon_Patch6_KT;
					damage.defaultArmorPenetration = (int)(item.Value.armorPenetrationBase * weapon_Patch5_KT);
				}
			}
			foreach (KeyValuePair<string, WeaponAndAmount> item2 in Settings_Main.Instance.Settings_KT.dictionary_Weapon_Damage_KT)
			{
				ThingDef projectile = item2.Value.projectile;
				typeof(ProjectileProperties).GetField("damageAmountBase", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(projectile.projectile, (int)((float)item2.Value.amount * Settings_Main.Instance.Settings_KT.Weapon_Patch5_KT));
				float weapon_Patch6_KT = Settings_Main.Instance.Settings_KT.Weapon_Patch6_KT;
				typeof(ProjectileProperties).GetField("armorPenetrationBase", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(projectile.projectile, item2.Value.armorPenetrationBase * weapon_Patch6_KT);
				if (projectile.projectile.explosionRadius > 0f)
				{
					float weapon_Patch7_KT = Settings_Main.Instance.Settings_KT.Weapon_Patch7_KT;
					projectile.projectile.explosionRadius = item2.Value.explosionRadius * weapon_Patch7_KT;
				}
			}
			foreach (KeyValuePair<string, WeaponAndCooldown> item3 in Settings_Main.Instance.Settings_KT.dictionary_Weapon_Cooldown_KT)
			{
				ThingDef weaponDef = item3.Value.weaponDef;
				weaponDef.statBases.Find((StatModifier x) => x.stat == StatDefOf.RangedWeapon_Cooldown).value = item3.Value.cooldownTime * Settings_Main.Instance.Settings_KT.Weapon_Patch8_KT;
				weaponDef.Verbs.First().warmupTime = item3.Value.warmupTime * Settings_Main.Instance.Settings_KT.Weapon_Patch9_KT;
			}
		}
	}

	private static readonly long baseline;

	static SimpleClass()
	{
		Ado.DoingList();
		Ado.Doing();
		baseline = DateTime.Now.Ticks;
		Log.Message(">-UF Heavy Industries Complete-<");
	}
}
