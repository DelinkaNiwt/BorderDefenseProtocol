using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace GD3
{
	public class PawnRenderNodeWorker_AnnihilatorTurretGun : PawnRenderNodeWorker
	{
		public override Quaternion RotationFor(PawnRenderNode node, PawnDrawParms parms)
		{
			Quaternion result = base.RotationFor(node, parms);
			if (node is PawnRenderNode_AnnihilatorTurretGun pawnRenderNode_TurretGun)
			{
				result *= pawnRenderNode_TurretGun.turretComp.curRotation.ToQuat();
			}
			return result;
		}
	}

}
