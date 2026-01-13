using System.Collections.Generic;
using HarmonyLib;
using RimWorld;

namespace ATFieldGenerator;

[HarmonyPatch(typeof(Tornado), "Tick")]
public static class Patch_Tornado_Tick
{
	public static bool Prefix(Tornado __instance)
	{
		if (__instance.Map != null && !__instance.Destroyed)
		{
			List<Comp_AbsoluteTerrorField> activeFields = ATFieldManager.Get(__instance.Map).activeFields;
			if (activeFields != null && activeFields.Count > 0)
			{
				for (int i = 0; i < activeFields.Count; i++)
				{
					Comp_AbsoluteTerrorField comp_AbsoluteTerrorField = activeFields[i];
					if (comp_AbsoluteTerrorField.Active && __instance.Position.InHorDistOf(comp_AbsoluteTerrorField.parent.Position, comp_AbsoluteTerrorField.radius))
					{
						FleckMaker.ThrowSmoke(__instance.Position.ToVector3Shifted(), __instance.Map, 4f);
						__instance.Destroy();
						return false;
					}
				}
			}
		}
		return true;
	}
}
