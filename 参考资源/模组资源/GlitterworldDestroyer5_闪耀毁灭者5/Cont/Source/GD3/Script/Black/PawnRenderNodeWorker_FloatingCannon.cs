using System;
using UnityEngine;
using Verse;

namespace GD3
{
    public class PawnRenderNodeWorker_FloatingCannon : PawnRenderNodeWorker
	{
		public override Quaternion RotationFor(PawnRenderNode node, PawnDrawParms parms)
		{
			Quaternion result = base.RotationFor(node, parms);
			if (node is PawnRenderNode_FloatingCannon pawnRenderNode_TurretGun)
			{
				result *= pawnRenderNode_TurretGun.turretComp.curRotation.ToQuat();
			}
			return result;
		}

		public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
        {
			Vector3 result = base.OffsetFor(node, parms, out pivot);
			pivot = PivotFor(node, parms);
			if (node is PawnRenderNode_FloatingCannon pawnRenderNode_TurretGun)
			{
				float angle = pawnRenderNode_TurretGun.turretComp.Angle;
				float offset = pawnRenderNode_TurretGun.turretComp.Props.offset;
				result += new Vector3(offset * (float)Mathf.Cos(angle * Mathf.Deg2Rad), 0, offset * (float)Mathf.Sin(angle * Mathf.Deg2Rad));
			}
			return result;
		}
	}
}
