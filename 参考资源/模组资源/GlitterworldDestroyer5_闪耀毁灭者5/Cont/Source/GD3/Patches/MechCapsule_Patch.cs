using System;
using HarmonyLib;
using Verse;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;

namespace GD3
{
	[HarmonyPatch(typeof(CompPawnSpawnOnWakeup), "Spawn")]
	public static class MechCapsule_Patch
	{
		public static void Postfix(CompPawnSpawnOnWakeup __instance)
		{
			AIAggressive ext = __instance.parent?.def?.GetModExtension<AIAggressive>();
			if (ext != null)
			{
				Thing t = __instance.parent;
				IntVec3 vec = t.Position;
				Map map = t.Map;
				t.Destroy(DestroyMode.Vanish);
				GenPlace.TryPlaceThing(ThingMaker.MakeThing(ext.emptyCapsuleDef), vec, map, ThingPlaceMode.Direct, null, null, default(Rot4));
			}
		}
	}
}