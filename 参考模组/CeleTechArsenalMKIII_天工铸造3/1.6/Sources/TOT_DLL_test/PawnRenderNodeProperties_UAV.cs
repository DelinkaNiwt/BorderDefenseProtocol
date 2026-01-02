using Verse;

namespace TOT_DLL_test;

public class PawnRenderNodeProperties_UAV : PawnRenderNodeProperties
{
	public bool drawUndrafted = true;

	public bool isApparel = true;

	public PawnRenderNodeProperties_UAV()
	{
		nodeClass = typeof(PawnRenderNode_UAV);
		workerClass = typeof(PawnRenderNodeWorker_UAV);
	}
}
