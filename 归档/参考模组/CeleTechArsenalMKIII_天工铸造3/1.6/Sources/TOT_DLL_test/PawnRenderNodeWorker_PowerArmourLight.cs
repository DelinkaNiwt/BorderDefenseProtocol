using UnityEngine;
using Verse;

namespace TOT_DLL_test;

public class PawnRenderNodeWorker_PowerArmourLight : PawnRenderNodeWorker_Apparel_Head
{
	public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
	{
		return base.CanDrawNow(node, parms);
	}

	public override Quaternion RotationFor(PawnRenderNode node, PawnDrawParms parms)
	{
		return base.RotationFor(node, parms);
	}

	public override void PreDraw(PawnRenderNode node, Material mat, PawnDrawParms parms)
	{
		node.MatPropBlock.SetColor(ShaderPropertyIDs.Color, parms.tint * mat.color);
	}
}
