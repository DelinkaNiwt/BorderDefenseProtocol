using Verse;

namespace TOT_DLL_test;

public class PawnRenderNodeProperties_HeadWithLight : PawnRenderNodeProperties
{
	public bool isApparel = true;

	public PawnRenderNodeProperties_HeadWithLight()
	{
		nodeClass = typeof(PawnRenderNode_HeadWithLight);
		workerClass = typeof(PawnRenderNodeWorker_HeadWithLight);
	}
}
