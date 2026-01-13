using System;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ATFieldGenerator;

[HarmonyPatch(typeof(GenSpawn), "Spawn", new Type[]
{
	typeof(Thing),
	typeof(IntVec3),
	typeof(Map),
	typeof(Rot4),
	typeof(WipeMode),
	typeof(bool),
	typeof(bool)
})]
public static class Patch_GenSpawn_Spawn
{
	private static Type groundSpawnerType;

	private static bool? hasAnomalyDLC;

	public static void Prefix(Thing newThing, ref IntVec3 loc, Map map)
	{
		if (map == null || newThing == null)
		{
			return;
		}
		if (!hasAnomalyDLC.HasValue)
		{
			groundSpawnerType = AccessTools.TypeByName("RimWorld.GroundSpawner");
			hasAnomalyDLC = groundSpawnerType != null;
		}
		bool flag = false;
		Type type = newThing.GetType();
		if (hasAnomalyDLC.Value && groundSpawnerType.IsAssignableFrom(type))
		{
			flag = true;
		}
		else if (type.Name.Contains("Tunnel"))
		{
			flag = true;
		}
		if (!flag)
		{
			return;
		}
		ATFieldManager aTFieldManager = ATFieldManager.Get(map);
		if (aTFieldManager == null || aTFieldManager.activeFields.Count == 0)
		{
			return;
		}
		for (int i = 0; i < aTFieldManager.activeFields.Count; i++)
		{
			Comp_AbsoluteTerrorField comp_AbsoluteTerrorField = aTFieldManager.activeFields[i];
			if (!comp_AbsoluteTerrorField.Active || !loc.InHorDistOf(comp_AbsoluteTerrorField.parent.Position, comp_AbsoluteTerrorField.radius))
			{
				continue;
			}
			Vector3 vector = comp_AbsoluteTerrorField.parent.Position.ToVector3Shifted();
			Vector3 vector2 = (loc.ToVector3Shifted() - vector).normalized;
			if (vector2 == Vector3.zero)
			{
				vector2 = Vector3.right;
			}
			IntVec3 intVec = (vector + vector2 * (comp_AbsoluteTerrorField.radius + 5f)).ToIntVec3().ClampInsideMap(map);
			bool flag2 = true;
			for (int j = 0; j < aTFieldManager.activeFields.Count; j++)
			{
				if (aTFieldManager.activeFields[j].Active && intVec.InHorDistOf(aTFieldManager.activeFields[j].parent.Position, aTFieldManager.activeFields[j].radius))
				{
					flag2 = false;
					break;
				}
			}
			if (flag2)
			{
				loc = intVec;
				FleckMaker.ThrowSmoke(intVec.ToVector3Shifted(), map, 2f);
			}
			else
			{
				loc = intVec;
			}
			break;
		}
	}
}
