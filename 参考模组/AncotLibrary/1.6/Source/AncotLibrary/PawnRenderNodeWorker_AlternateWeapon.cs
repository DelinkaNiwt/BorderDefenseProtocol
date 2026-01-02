using UnityEngine;
using Verse;

namespace AncotLibrary;

public class PawnRenderNodeWorker_AlternateWeapon : PawnRenderNodeWorker
{
	public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
	{
		if (!base.CanDrawNow(node, parms))
		{
			return false;
		}
		HediffComp_AlternateWeapon hediffComp_AlternateWeapon = ((node is PawnRenderNode_AlternateWeapon pawnRenderNode_AlternateWeapon) ? pawnRenderNode_AlternateWeapon.compAlternateWeapon : null);
		if (hediffComp_AlternateWeapon != null && !hediffComp_AlternateWeapon.innerContainer.NullOrEmpty())
		{
			return true;
		}
		return false;
	}

	public override Vector3 ScaleFor(PawnRenderNode node, PawnDrawParms parms)
	{
		HediffComp_AlternateWeapon hediffComp_AlternateWeapon = ((node is PawnRenderNode_AlternateWeapon pawnRenderNode_AlternateWeapon) ? pawnRenderNode_AlternateWeapon.compAlternateWeapon : null);
		if (hediffComp_AlternateWeapon == null)
		{
			return Vector3.zero;
		}
		Vector3 vector = hediffComp_AlternateWeapon.innerContainer[0].Graphic.drawSize;
		Vector3 result = base.ScaleFor(node, parms);
		result.x *= vector.x;
		result.z *= vector.y;
		return result;
	}

	public override Quaternion RotationFor(PawnRenderNode node, PawnDrawParms parms)
	{
		Quaternion result = base.RotationFor(node, parms);
		PawnRenderNode_AlternateWeapon pawnRenderNode_AlternateWeapon = node as PawnRenderNode_AlternateWeapon;
		HediffComp_AlternateWeapon hediffComp_AlternateWeapon = pawnRenderNode_AlternateWeapon?.compAlternateWeapon;
		if (pawnRenderNode_AlternateWeapon != null && hediffComp_AlternateWeapon != null && !hediffComp_AlternateWeapon.innerContainer.NullOrEmpty())
		{
			float num = hediffComp_AlternateWeapon?.innerContainer[0].def.equippedAngleOffset ?? 0f;
			if (num > 0f)
			{
				num = 0f - num;
			}
			if (parms.pawn.Rotation == Rot4.North || parms.pawn.Rotation == Rot4.East)
			{
				return result *= (0f - num).ToQuat();
			}
			result *= num.ToQuat();
		}
		return result;
	}
}
