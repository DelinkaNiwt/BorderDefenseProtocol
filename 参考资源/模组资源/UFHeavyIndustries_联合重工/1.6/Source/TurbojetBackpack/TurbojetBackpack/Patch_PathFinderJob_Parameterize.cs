using HarmonyLib;
using Verse;

namespace TurbojetBackpack;

[HarmonyPatch(typeof(PathFinder), "ParameterizePathJob")]
public static class Patch_PathFinderJob_Parameterize
{
	public static void Postfix(ref PathFinderJob job, PathRequest request)
	{
		Pawn pawn = request.pawn;
		if (pawn != null && TurbojetGlobal.IsFlightActive(pawn))
		{
			TraverseParms parms = TraverseParms.For(TraverseMode.PassAllDestroyableThings, Danger.Deadly, canBashDoors: false, alwaysUseAvoidGrid: false, canBashFences: false, avoidPersistentDanger: false);
			job.traverseParams = PathFinder.UnmanagedGridTraverseParams.For(parms);
			job.heuristicStrength = 1f;
		}
	}
}
