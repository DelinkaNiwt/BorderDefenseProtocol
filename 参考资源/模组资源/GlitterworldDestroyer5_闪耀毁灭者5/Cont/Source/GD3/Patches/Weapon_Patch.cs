using System;
using HarmonyLib;
using Verse;
using RimWorld;
using System.Collections.Generic;

namespace GD3
{
	[HarmonyPatch(typeof(Building_TurretGun), "Tick")]
	public static class WeaponTick_Patch
	{
		public static void Postfix(Building_TurretGun __instance)
		{
			if (__instance.gun != null)
            {
				CompSecVerb comp = __instance.gun.TryGetComp<CompSecVerb>();
				if (comp != null)
				{
					comp.VerbTick();
				}
			}
		}
	}

	[HarmonyPatch(typeof(Building_TurretGun), "BurstCooldownTime")]
	public static class TurretWeapon_Patch
	{
		public static bool Prefix(Building_TurretGun __instance, ref float __result)
		{
			if (__instance.GetStatValue(GDDefOf.RangedCooldownFactorBuilding) != 1.0f)
			{
				float factor = __instance.GetStatValue(GDDefOf.RangedCooldownFactorBuilding);
				if (__instance.def.building.turretBurstCooldownTime >= 0f && __instance.def.defName != "Turret_GiantAutoMortar")
				{
					__result = __instance.def.building.turretBurstCooldownTime * factor;
					return false;
				}
				__result = __instance.AttackVerb.verbProps.defaultCooldownTime * factor;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Building_TurretGun), "BeginBurst")]
	public static class TurretConsumePower_Patch
	{
		public static bool Prefix(Building_TurretGun __instance)
		{
			if (__instance.def.defName == "PlayerTurret_GiantChargeBlaster" && GDSettings.TurretConsumePower > 0)
            {
				CompPowerTrader comp = __instance.TryGetComp<CompPowerTrader>();
				if (comp != null && comp.PowerOn)
                {
					List<CompPowerBattery> batteryComps = comp.PowerNet.batteryComps;
					if (batteryComps.Count <= 0 || comp.PowerNet.CurrentStoredEnergy() <= GDSettings.TurretConsumePower)
                    {
						return false;
                    }
					float num = GDSettings.TurretConsumePower / batteryComps.Count;
					for (int i = 0; i < batteryComps.Count; i++)
					{
						float pct = (batteryComps[i].StoredEnergy - num) / batteryComps[i].Props.storedEnergyMax;
						batteryComps[i].SetStoredEnergyPct(pct);
					}
				}
            }
			return true;
		}
	}
}
