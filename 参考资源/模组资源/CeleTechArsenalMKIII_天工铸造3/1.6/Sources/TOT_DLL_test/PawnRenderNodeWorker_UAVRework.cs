using UnityEngine;
using Verse;

namespace TOT_DLL_test;

public class PawnRenderNodeWorker_UAVRework : PawnRenderNodeWorker
{
	public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
	{
		if (!base.CanDrawNow(node, parms))
		{
			return false;
		}
		PawnRenderNode_UAVRework pawnRenderNode_UAVRework = node as PawnRenderNode_UAVRework;
		return pawnRenderNode_UAVRework.turretComp != null && pawnRenderNode_UAVRework.turretComp.PawnOwner != null && pawnRenderNode_UAVRework.turretComp.Released;
	}

	public override float LayerFor(PawnRenderNode node, PawnDrawParms parms)
	{
		float num = (node.Props.drawData?.LayerForRot(parms.facing, node.Props.baseLayer) ?? node.Props.baseLayer) + node.debugLayerOffset;
		PawnRenderNode_UAVRework pawnRenderNode_UAVRework = node as PawnRenderNode_UAVRework;
		if (pawnRenderNode_UAVRework.turretComp.currentPosition.z < parms.pawn.DrawPos.z)
		{
			num += 100f;
		}
		return num;
	}

	public override Quaternion RotationFor(PawnRenderNode node, PawnDrawParms parms)
	{
		return Quaternion.AngleAxis(0f, Vector3.up);
	}

	public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
	{
		PawnRenderNode_UAVRework pawnRenderNode_UAVRework = node as PawnRenderNode_UAVRework;
		Vector3 zero = Vector3.zero;
		pivot = PivotFor(node, parms);
		if (node.Props.drawData != null)
		{
			zero += node.Props.drawData.OffsetForRot(parms.facing);
		}
		zero += node.DebugOffset;
		if (!parms.flags.FlagSet(PawnRenderFlags.Portrait) && node.TryGetAnimationOffset(parms, out var offset))
		{
			zero += offset;
		}
		return zero + pawnRenderNode_UAVRework.turretComp.currentPosition - parms.pawn.DrawPos;
	}

	protected override Vector3 PivotFor(PawnRenderNode node, PawnDrawParms parms)
	{
		return parms.pawn.DrawPos;
	}
}
