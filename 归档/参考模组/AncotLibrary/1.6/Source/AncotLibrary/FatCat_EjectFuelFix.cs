using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

[StaticConstructorOnStartup]
[HarmonyPatch(typeof(CompRefuelable))]
[HarmonyPatch("EjectFuel")]
public static class FatCat_EjectFuelFix
{
	[HarmonyPrefix]
	public static bool Prefix(CompRefuelable __instance, ref float ___fuel)
	{
		float fuelMultiplierCurrentDifficulty = __instance.Props.FuelMultiplierCurrentDifficulty;
		if (fuelMultiplierCurrentDifficulty != 1f && fuelMultiplierCurrentDifficulty > 0f)
		{
			ThingDef thingDef = __instance.Props.fuelFilter.AllowedThingDefs.First();
			int num = Mathf.FloorToInt(___fuel / fuelMultiplierCurrentDifficulty);
			while (num > 0)
			{
				Thing thing = ThingMaker.MakeThing(thingDef);
				thing.stackCount = Mathf.Min(num, thingDef.stackLimit);
				num -= thing.stackCount;
				GenPlace.TryPlaceThing(thing, __instance.parent.Position, __instance.parent.Map, ThingPlaceMode.Near);
				thing.SetForbidden(value: true);
			}
			___fuel = 0f;
			Notify_RanOutOfFuel(__instance);
			return false;
		}
		return true;
	}

	public static void Notify_RanOutOfFuel(CompRefuelable comp)
	{
		if (comp.Props.destroyOnNoFuel)
		{
			comp.parent.Destroy();
		}
		comp.parent.BroadcastCompSignal("RanOutOfFuel");
	}
}
