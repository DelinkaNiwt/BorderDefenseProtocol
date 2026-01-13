using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ATFieldGenerator;

[HarmonyPatch]
public static class Patch_GroundSpawner_Tick
{
	private static MethodBase TargetMethod()
	{
		return AccessTools.Method(AccessTools.TypeByName("RimWorld.GroundSpawner"), "Tick");
	}

	public static bool Prepare()
	{
		return AccessTools.TypeByName("RimWorld.GroundSpawner") != null;
	}

	public static bool Prefix(Thing __instance)
	{
		if (__instance.Destroyed || __instance.Map == null)
		{
			return true;
		}
		if (!__instance.IsHashIntervalTick(30))
		{
			return true;
		}
		ATFieldManager aTFieldManager = ATFieldManager.Get(__instance.Map);
		if (aTFieldManager == null || aTFieldManager.activeFields.Count == 0)
		{
			return true;
		}
		for (int i = 0; i < aTFieldManager.activeFields.Count; i++)
		{
			Comp_AbsoluteTerrorField comp_AbsoluteTerrorField = aTFieldManager.activeFields[i];
			if (!comp_AbsoluteTerrorField.Active || !__instance.Position.InHorDistOf(comp_AbsoluteTerrorField.parent.Position, comp_AbsoluteTerrorField.radius))
			{
				continue;
			}
			Vector3 vector = comp_AbsoluteTerrorField.parent.Position.ToVector3Shifted();
			Vector3 vector2 = (__instance.Position.ToVector3Shifted() - vector).normalized;
			if (vector2 == Vector3.zero)
			{
				vector2 = Vector3.right;
			}
			IntVec3 loc = (vector + vector2 * (comp_AbsoluteTerrorField.radius + 5f)).ToIntVec3().ClampInsideMap(__instance.Map);
			bool flag = true;
			for (int j = 0; j < aTFieldManager.activeFields.Count; j++)
			{
				if (aTFieldManager.activeFields[j].Active && loc.InHorDistOf(aTFieldManager.activeFields[j].parent.Position, aTFieldManager.activeFields[j].radius))
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				FleckMaker.ThrowMicroSparks(__instance.Position.ToVector3Shifted(), __instance.Map);
				Map map = __instance.Map;
				__instance.DeSpawn();
				GenSpawn.Spawn(__instance, loc, map);
				FleckMaker.ThrowSmoke(loc.ToVector3Shifted(), map, 2f);
			}
			else
			{
				FleckMaker.ThrowMicroSparks(__instance.Position.ToVector3Shifted(), __instance.Map);
				if (!__instance.def.destroyable)
				{
					__instance.DeSpawn();
				}
				else
				{
					__instance.Destroy();
				}
			}
			return false;
		}
		return true;
	}
}
