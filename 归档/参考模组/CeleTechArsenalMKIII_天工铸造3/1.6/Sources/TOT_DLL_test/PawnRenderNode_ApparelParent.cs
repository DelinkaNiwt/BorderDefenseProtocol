using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test;

public class PawnRenderNode_ApparelParent : PawnRenderNode_Apparel
{
	public PawnRenderNode_ApparelParent(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree, Apparel apparel)
		: base(pawn, props, tree, apparel)
	{
		base.apparel = apparel;
		useHeadMesh = true;
	}

	public override GraphicMeshSet MeshSetFor(Pawn pawn)
	{
		if (props.overrideMeshSize.HasValue)
		{
			return MeshPool.GetMeshSetForSize(props.overrideMeshSize.Value.x, props.overrideMeshSize.Value.y);
		}
		return HumanlikeMeshPoolUtility.GetHumanlikeHeadSetForPawn(pawn);
	}

	public override Color ColorFor(Pawn pawn)
	{
		return apparel.DrawColor;
	}
}
