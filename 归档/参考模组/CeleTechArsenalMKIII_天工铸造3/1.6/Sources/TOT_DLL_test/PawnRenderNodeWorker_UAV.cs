using UnityEngine;
using Verse;

namespace TOT_DLL_test;

public class PawnRenderNodeWorker_UAV : PawnRenderNodeWorker
{
	public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
	{
		if (!base.CanDrawNow(node, parms))
		{
			return false;
		}
		PawnRenderNode_UAV pawnRenderNode_UAV = node as PawnRenderNode_UAV;
		PawnRenderNodeProperties_UAV pawnRenderNodeProperties_UAV = node.Props as PawnRenderNodeProperties_UAV;
		pawnRenderNode_UAV.turretComp = pawnRenderNode_UAV.apparel.TryGetComp<Comp_UAV>();
		return pawnRenderNode_UAV.turretComp != null && pawnRenderNode_UAV.turretComp.CanShoot;
	}

	public override Quaternion RotationFor(PawnRenderNode node, PawnDrawParms parms)
	{
		return base.RotationFor(node, parms);
	}

	public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
	{
		PawnRenderNode_UAV pawnRenderNode_UAV = node as PawnRenderNode_UAV;
		PawnRenderNodeProperties_UAV pawnRenderNodeProperties_UAV = node.Props as PawnRenderNodeProperties_UAV;
		if (pawnRenderNodeProperties_UAV.isApparel)
		{
			pawnRenderNode_UAV.turretComp = pawnRenderNode_UAV.apparel.TryGetComp<Comp_UAV>();
		}
		if (pawnRenderNode_UAV != null && pawnRenderNode_UAV.turretComp != null)
		{
			Vector3 vector = new Vector3(pawnRenderNode_UAV.turretComp.PosX, 0f, pawnRenderNode_UAV.turretComp.PosY);
			return base.OffsetFor(node, parms, out pivot) + vector;
		}
		return base.OffsetFor(node, parms, out pivot);
	}
}
