using Verse;

namespace TOT_DLL_test;

public class PawnRenderNodeProperties_PowerArmourLight : PawnRenderNodeProperties
{
	public bool isApparel = true;

	public PawnRenderNodeProperties_PowerArmourLight()
	{
		nodeClass = typeof(PawnRenderNode_PowerArmourLight);
		workerClass = typeof(PawnRenderNodeWorker_PowerArmourLight);
	}
}
