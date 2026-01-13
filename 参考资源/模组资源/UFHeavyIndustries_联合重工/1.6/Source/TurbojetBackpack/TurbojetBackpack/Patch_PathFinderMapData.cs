using HarmonyLib;
using Verse;

namespace TurbojetBackpack;

[HarmonyPatch(typeof(PathFinderMapData), "ParameterizeGridJob")]
public static class Patch_PathFinderMapData
{
	public static void Postfix(PathRequest request, ref PathGridJob job)
	{
		Pawn pawn = request.pawn;
		if (pawn != null && TurbojetGlobal.IsFlightActive(pawn))
		{
			job.pathGridDirect = TurbojetGlobal.GetFakeWalkableGrid(pawn.Map.cellIndices.NumGridCells).AsReadOnly();
		}
	}
}
