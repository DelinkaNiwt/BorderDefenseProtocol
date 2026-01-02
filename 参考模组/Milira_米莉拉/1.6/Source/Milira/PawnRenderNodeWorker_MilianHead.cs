using UnityEngine;
using Verse;

namespace Milira;

public class PawnRenderNodeWorker_MilianHead : PawnRenderNodeWorker_FlipWhenCrawling
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
		return base.OffsetFor(node, parms, out pivot) + parms.pawn.Drawer.renderer.BaseHeadOffsetAt(parms.facing);
	}

	public override Quaternion RotationFor(PawnRenderNode node, PawnDrawParms parms)
	{
		Quaternion result = base.RotationFor(node, parms);
		if (!parms.Portrait && parms.pawn.Crawling)
		{
			result *= PawnRenderUtility.CrawlingHeadAngle(parms.facing).ToQuat();
			if (parms.flipHead)
			{
				result *= 180f.ToQuat();
			}
		}
		if (parms.pawn.IsShambler && parms.pawn.mutant != null && parms.pawn.mutant.HasTurned && !parms.pawn.Dead)
		{
			result *= Quaternion.Euler(Vector3.up * ((parms.pawn.mutant.Hediff as Hediff_Shambler)?.headRotation ?? 0f));
		}
		return result;
	}
}
