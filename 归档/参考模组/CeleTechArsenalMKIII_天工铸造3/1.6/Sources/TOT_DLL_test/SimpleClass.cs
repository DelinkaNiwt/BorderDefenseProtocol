using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;

namespace TOT_DLL_test;

[StaticConstructorOnStartup]
internal class SimpleClass
{
	public class Ado
	{
		public static void DoingList()
		{
			foreach (ThingDef allDef in DefDatabase<ThingDef>.AllDefs)
			{
				if (allDef.weaponTags != null && allDef.IsRangedWeapon && allDef.weaponTags.Exists((string x) => x.EndsWith("_CMCf")) && allDef.Verbs != null)
				{
					float warmupTime = allDef.Verbs.First().warmupTime;
					float value = allDef.statBases.Find((StatModifier x) => x.stat == StatDefOf.RangedWeapon_Cooldown).value;
					WeaponAndCooldown weaponAndCooldown = new WeaponAndCooldown();
					weaponAndCooldown.weaponDef = allDef;
					weaponAndCooldown.cooldownTime = value;
					weaponAndCooldown.warmupTime = warmupTime;
					Settings_CMC_Main.Instance.settings_CMC.dictionary_Weapon_Cooldown_CMC[allDef.defName] = weaponAndCooldown;
					VerbProperties verbProperties = allDef.Verbs.Find((VerbProperties x) => x.defaultProjectile != null);
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
						Settings_CMC_Main.Instance.settings_CMC.dictionary_Weapon_Damage_CMC[verbProperties.defaultProjectile.defName] = weaponAndAmount;
					}
					if (allDef.comps.Find((CompProperties x) => x is CompProperties_SecondaryVerb_Rework) is CompProperties_SecondaryVerb_Rework compProperties_SecondaryVerb_Rework)
					{
						ThingDef defaultProjectile2 = compProperties_SecondaryVerb_Rework.verbProps.defaultProjectile;
						int amount2 = (int)typeof(ProjectileProperties).GetField("damageAmountBase", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(defaultProjectile2.projectile);
						float armorPenetrationBase2 = (float)typeof(ProjectileProperties).GetField("armorPenetrationBase", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(defaultProjectile2.projectile);
						WeaponAndAmount weaponAndAmount2 = new WeaponAndAmount();
						weaponAndAmount2.projectile = defaultProjectile2;
						weaponAndAmount2.amount = amount2;
						weaponAndAmount2.armorPenetrationBase = armorPenetrationBase2;
						weaponAndAmount2.explosionRadius = defaultProjectile2.projectile.explosionRadius;
						Settings_CMC_Main.Instance.settings_CMC.dictionary_Weapon_Damage_CMC[compProperties_SecondaryVerb_Rework.verbProps.defaultProjectile.defName] = weaponAndAmount2;
					}
					if (allDef.comps.Find((CompProperties x) => x is CompProperties_LaserData_Sustain) is CompProperties_LaserData_Sustain { DamageNum: var damageNum, DamageArmorPenetration: var damageArmorPenetration } compProperties_LaserData_Sustain)
					{
						WeaponAndAmount weaponAndAmount3 = new WeaponAndAmount();
						weaponAndAmount3.projectile = allDef;
						weaponAndAmount3.amount = damageNum;
						weaponAndAmount3.ScatterExplosionDamage = compProperties_LaserData_Sustain.ScatterExplosionDamage;
						weaponAndAmount3.ScatterExplosionArmorPenetration = compProperties_LaserData_Sustain.ScatterExplosionArmorPenetration;
						weaponAndAmount3.armorPenetrationBase = damageArmorPenetration;
						weaponAndAmount3.explosionRadius = compProperties_LaserData_Sustain.ScatterExplosionRadius;
						Settings_CMC_Main.Instance.settings_CMC.dictionary_Weapon_Damage_CMC[allDef.defName] = weaponAndAmount3;
					}
					else if (allDef.comps.Find((CompProperties x) => x is CompProperties_LaserData_Instant) is CompProperties_LaserData_Instant { DamageNum: var damageNum2, DamageArmorPenetration: var damageArmorPenetration2 } compProperties_LaserData_Instant)
					{
						WeaponAndAmount weaponAndAmount4 = new WeaponAndAmount();
						weaponAndAmount4.projectile = allDef;
						weaponAndAmount4.amount = damageNum2;
						weaponAndAmount4.ScatterExplosionDamage = compProperties_LaserData_Instant.ScatterExplosionDamage;
						weaponAndAmount4.ScatterExplosionArmorPenetration = compProperties_LaserData_Instant.ScatterExplosionArmorPenetration;
						weaponAndAmount4.armorPenetrationBase = damageArmorPenetration2;
						weaponAndAmount4.explosionRadius = compProperties_LaserData_Instant.ScatterExplosionRadius;
						Settings_CMC_Main.Instance.settings_CMC.dictionary_Weapon_Damage_CMC[allDef.defName] = weaponAndAmount4;
					}
				}
			}
		}

		public static void Doing()
		{
			List<string> list = new List<string>();
			foreach (KeyValuePair<string, WeaponAndAmount> item in Settings_CMC_Main.Instance.settings_CMC.dictionary_Weapon_Damage_CMC)
			{
				ThingDef projectile = item.Value.projectile;
				if (projectile.projectile != null)
				{
					typeof(ProjectileProperties).GetField("damageAmountBase", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(projectile.projectile, (int)((float)item.Value.amount * Settings_CMC_Main.Instance.settings_CMC.Weapon_Patch5_CMC));
					float weapon_Patch6_CMC = Settings_CMC_Main.Instance.settings_CMC.Weapon_Patch6_CMC;
					typeof(ProjectileProperties).GetField("armorPenetrationBase", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(projectile.projectile, item.Value.armorPenetrationBase * weapon_Patch6_CMC);
					if (projectile.projectile.explosionRadius > 0f)
					{
						float weapon_Patch7_CMC = Settings_CMC_Main.Instance.settings_CMC.Weapon_Patch7_CMC;
						projectile.projectile.explosionRadius = item.Value.explosionRadius * weapon_Patch7_CMC;
					}
				}
				else if (projectile.comps.Find((CompProperties x) => x is CompProperties_LaserData_Sustain) is CompProperties_LaserData_Sustain compProperties_LaserData_Sustain)
				{
					compProperties_LaserData_Sustain.DamageNum = (int)((float)item.Value.amount * Settings_CMC_Main.Instance.settings_CMC.Weapon_Patch5_CMC);
					compProperties_LaserData_Sustain.DamageArmorPenetration = (int)(item.Value.armorPenetrationBase * Settings_CMC_Main.Instance.settings_CMC.Weapon_Patch6_CMC);
					if (compProperties_LaserData_Sustain.ScatterExplosionDef != null)
					{
						float weapon_Patch7_CMC2 = Settings_CMC_Main.Instance.settings_CMC.Weapon_Patch7_CMC;
						compProperties_LaserData_Sustain.ScatterExplosionRadius = item.Value.explosionRadius * weapon_Patch7_CMC2;
						compProperties_LaserData_Sustain.ScatterExplosionDamage = (int)(item.Value.ScatterExplosionDamage * Settings_CMC_Main.Instance.settings_CMC.Weapon_Patch5_CMC);
						if (compProperties_LaserData_Sustain.ScatterExplosionDamage <= 0)
						{
							compProperties_LaserData_Sustain.ScatterExplosionDamage = 1;
						}
						compProperties_LaserData_Sustain.ScatterExplosionArmorPenetration = (int)(item.Value.ScatterExplosionArmorPenetration * Settings_CMC_Main.Instance.settings_CMC.Weapon_Patch6_CMC);
					}
				}
				else if (projectile.comps.Find((CompProperties x) => x is CompProperties_LaserData_Instant) is CompProperties_LaserData_Instant compProperties_LaserData_Instant)
				{
					compProperties_LaserData_Instant.DamageNum = (int)((float)item.Value.amount * Settings_CMC_Main.Instance.settings_CMC.Weapon_Patch5_CMC);
					compProperties_LaserData_Instant.DamageArmorPenetration = (int)(item.Value.armorPenetrationBase * Settings_CMC_Main.Instance.settings_CMC.Weapon_Patch6_CMC);
					if (compProperties_LaserData_Instant.ScatterExplosionDef != null)
					{
						float weapon_Patch7_CMC3 = Settings_CMC_Main.Instance.settings_CMC.Weapon_Patch7_CMC;
						compProperties_LaserData_Instant.ScatterExplosionRadius = item.Value.explosionRadius * weapon_Patch7_CMC3;
						compProperties_LaserData_Instant.ScatterExplosionDamage = (int)(item.Value.ScatterExplosionDamage * Settings_CMC_Main.Instance.settings_CMC.Weapon_Patch5_CMC);
						if (compProperties_LaserData_Instant.ScatterExplosionDamage <= 0)
						{
							compProperties_LaserData_Instant.ScatterExplosionDamage = 1;
						}
						compProperties_LaserData_Instant.ScatterExplosionArmorPenetration = (int)(item.Value.ScatterExplosionArmorPenetration * Settings_CMC_Main.Instance.settings_CMC.Weapon_Patch6_CMC);
					}
				}
				foreach (KeyValuePair<string, WeaponAndCooldown> item2 in Settings_CMC_Main.Instance.settings_CMC.dictionary_Weapon_Cooldown_CMC)
				{
					ThingDef weaponDef = item2.Value.weaponDef;
					weaponDef.statBases.Find((StatModifier x) => x.stat == StatDefOf.RangedWeapon_Cooldown).value = item2.Value.cooldownTime * Settings_CMC_Main.Instance.settings_CMC.Weapon_Patch8_CMC;
					weaponDef.Verbs.First().warmupTime = item2.Value.warmupTime * Settings_CMC_Main.Instance.settings_CMC.Weapon_Patch9_CMC;
				}
				foreach (ThingDef allDef in DefDatabase<ThingDef>.AllDefs)
				{
					if (allDef.weaponTags == null || !allDef.weaponTags.Exists((string x) => x.EndsWith("_CMCf")))
					{
						continue;
					}
					if (!Settings_CMC_Main.Instance.settings_CMC.Weapon_Patch1_CMC)
					{
						if (allDef.weaponTags.Exists((string t) => t == "GunHeavy"))
						{
							list.Clear();
							foreach (string weaponTag in allDef.weaponTags)
							{
								if (weaponTag != "GunHeavy")
								{
									if (!weaponTag.EndsWith("_CMCf"))
									{
										list.Add(weaponTag + "_CMC_");
									}
									else
									{
										list.Add(weaponTag);
									}
								}
							}
							allDef.weaponTags.Clear();
							foreach (string item3 in list)
							{
								allDef.weaponTags.Add(item3);
							}
							allDef.weaponTags.Add("GunHeavy_CMC_");
						}
					}
					else if (allDef.weaponTags.Exists((string t) => t == "GunHeavy_CMC_") & allDef.weaponTags.Exists((string t) => t != "GunSuper_CMC"))
					{
						list.Clear();
						foreach (string weaponTag2 in allDef.weaponTags)
						{
							if (weaponTag2 != "GunHeavy_CMC_")
							{
								if (!weaponTag2.EndsWith("_CMCf"))
								{
									list.Add(weaponTag2.Substring(0, weaponTag2.Length - 5));
								}
								else
								{
									list.Add(weaponTag2);
								}
							}
						}
						allDef.weaponTags.Clear();
						foreach (string item4 in list)
						{
							allDef.weaponTags.Add(item4);
						}
						allDef.weaponTags.Add("GunHeavy");
					}
					if (!Settings_CMC_Main.Instance.settings_CMC.Weapon_Patch3_CMC)
					{
						if (allDef.weaponTags.Exists((string t) => t != "GunSuper_CMC") & allDef.weaponTags.Exists((string t) => t != "GunHeavy") & allDef.weaponTags.Exists((string t) => t != "GunHeavy_CMC_"))
						{
							list.Clear();
							foreach (string weaponTag3 in allDef.weaponTags)
							{
								if (weaponTag3.Length <= 4)
								{
									continue;
								}
								if (weaponTag3.Substring(weaponTag3.Length - 5) != "_CMC_")
								{
									if (!weaponTag3.EndsWith("_CMCf"))
									{
										list.Add(weaponTag3 + "_CMC_");
									}
									else
									{
										list.Add(weaponTag3);
									}
								}
								else
								{
									list.Add(weaponTag3);
								}
							}
						}
						allDef.weaponTags.Clear();
						foreach (string item5 in list)
						{
							allDef.weaponTags.Add(item5);
						}
					}
					else
					{
						if (!(allDef.weaponTags.Exists((string t) => t != "GunSuper_CMC") & allDef.weaponTags.Exists((string t) => t != "GunHeavy") & allDef.weaponTags.Exists((string t) => t != "GunHeavy_CMC_")))
						{
							continue;
						}
						list.Clear();
						foreach (string weaponTag4 in allDef.weaponTags)
						{
							if (weaponTag4.EndsWith("_CMC_"))
							{
								list.Add(weaponTag4.Substring(0, weaponTag4.Length - 5));
							}
							else
							{
								list.Add(weaponTag4);
							}
						}
						allDef.weaponTags.Clear();
						foreach (string item6 in list)
						{
							allDef.weaponTags.Add(item6);
						}
					}
				}
			}
		}
	}

	private static readonly long baseline;

	static SimpleClass()
	{
		Ado.DoingList();
		Ado.Doing();
		baseline = DateTime.Now.Ticks;
	}
}
