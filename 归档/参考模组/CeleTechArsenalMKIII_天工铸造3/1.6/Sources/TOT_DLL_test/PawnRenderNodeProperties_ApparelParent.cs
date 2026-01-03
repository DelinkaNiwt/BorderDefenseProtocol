using Verse;

namespace TOT_DLL_test;

public class PawnRenderNodeProperties_ApparelParent : PawnRenderNodeProperties
{
	public PawnRenderNodeProperties_ApparelParent()
	{
		useGraphic = false;
		nodeClass = typeof(PawnRenderNode_ApparelParent);
		colorType = AttachmentColorType.Custom;
	}
}
