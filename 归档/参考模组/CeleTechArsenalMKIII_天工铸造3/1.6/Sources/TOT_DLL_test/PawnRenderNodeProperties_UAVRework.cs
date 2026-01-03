using Verse;

namespace TOT_DLL_test;

public class PawnRenderNodeProperties_UAVRework : PawnRenderNodeProperties
{
	public bool drawUndrafted = true;

	public bool isApparel = true;

	public bool useBodyPartAnchor = false;

	public bool useforcedColor = true;

	public PawnRenderNodeProperties_UAVRework()
	{
		nodeClass = typeof(PawnRenderNode_UAVRework);
		workerClass = typeof(PawnRenderNodeWorker_UAVRework);
	}
}
