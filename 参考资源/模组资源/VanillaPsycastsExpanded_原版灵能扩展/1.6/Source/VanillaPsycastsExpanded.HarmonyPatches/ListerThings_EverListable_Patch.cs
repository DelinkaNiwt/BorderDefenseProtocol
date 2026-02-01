using HarmonyLib;
using Verse;

namespace VanillaPsycastsExpanded.HarmonyPatches;

[HarmonyPatch(typeof(ListerThings), "EverListable")]
public static class ListerThings_EverListable_Patch
{
	public static void Postfix(ThingDef def, ref bool __result)
	{
		if (def.CanBeSaved())
		{
			__result = true;
		}
	}

	public static bool CanBeSaved(this ThingDef def)
	{
		if (def != null && (typeof(MoteAttachedScaled).IsAssignableFrom(def.thingClass) || typeof(MoteAttachedMovingAround).IsAssignableFrom(def.thingClass) || typeof(MoteAttachedOneTime).IsAssignableFrom(def.thingClass)))
		{
			return true;
		}
		return false;
	}
}
