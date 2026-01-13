using System;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace TurbojetBackpack;

[HarmonyPatch(typeof(PathFinder), "CreateRequest", new Type[]
{
	typeof(IntVec3),
	typeof(LocalTargetInfo),
	typeof(IntVec3?),
	typeof(TraverseParms),
	typeof(PathFinderCostTuning),
	typeof(PathEndMode),
	typeof(Pawn),
	typeof(PathRequest.IPathGridCustomizer)
})]
public static class Patch_PathFinder_CreateRequest
{
	private static readonly PathFinderCostTuning FlightTuning = new PathFinderCostTuning
	{
		costBlockedWallBase = 0,
		costBlockedWallExtraPerHitPoint = 0f,
		costBlockedDoor = 0,
		costBlockedDoorPerHitPoint = 0f,
		costWater = 0,
		costOffLordWalkGrid = 0
	};

	public static void Prefix(IntVec3 start, ref TraverseParms traverseParms, ref PathFinderCostTuning mtuning, Pawn pawn, ref PathRequest.IPathGridCustomizer customizer)
	{
		if (TurbojetGlobal.IsFlightActive(pawn))
		{
			traverseParms = TraverseParms.For(pawn, Danger.Deadly, TraverseMode.PassAllDestroyableThings);
			mtuning = FlightTuning;
			if (customizer == null)
			{
				customizer = new FlightPathGridCustomizer(pawn.Map, TurbojetGlobal.GetEffectiveMode(pawn));
			}
		}
	}
}
