using UnityEngine;
using Verse;

namespace AncotLibrary;

public class PawnRenderNodeWorker_PhysicalShield_Apparel : PawnRenderNodeWorker_FlipWhenCrawling
{
	public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
	{
		if (base.CanDrawNow(node, parms))
		{
			return true;
		}
		return false;
	}

	public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
	{
		pivot = PivotFor(node, parms);
		CompPhysicalShield shieldComp = ((PawnRenderNode_PhysicalShield_Apparel)node).shieldComp;
		return shieldComp.impactAngleVect * 0.02f;
	}

	public override float LayerFor(PawnRenderNode node, PawnDrawParms parms)
	{
		CompPhysicalShield shieldComp = ((PawnRenderNode_PhysicalShield_Apparel)node).shieldComp;
		Pawn pawn = parms.pawn;
		float num = 0f;
		if (shieldComp == null)
		{
			return 0f;
		}
		switch (shieldComp.ShieldState)
		{
		case An_ShieldState.Active:
			num = ((pawn.Rotation.AsAngle != 0f) ? 75f : (-5f));
			break;
		case An_ShieldState.Resetting:
		case An_ShieldState.Ready:
			num = ((pawn.Rotation.AsAngle != 180f) ? (-5f) : 75f);
			break;
		default:
			num = ((pawn.Rotation.AsAngle != 180f) ? 90f : (-5f));
			break;
		}
		return num + node.debugLayerOffset;
	}
}
