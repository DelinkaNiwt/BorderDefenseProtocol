using RimWorld;
using UnityEngine;
using Verse;

namespace Milira;

public class PawnRenderNodeWorker_MilianApparel_Body : PawnRenderNodeWorker_Body
{
	public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
	{
		if (!base.CanDrawNow(node, parms))
		{
			return false;
		}
		if (!parms.flags.FlagSet(PawnRenderFlags.Clothes))
		{
			return false;
		}
		return true;
	}

	public override Vector3 OffsetFor(PawnRenderNode n, PawnDrawParms parms, out Vector3 pivot)
	{
		Vector3 result = base.OffsetFor(n, parms, out pivot);
		PawnRenderNode_MilianApparel pawnRenderNode_MilianApparel = (PawnRenderNode_MilianApparel)n;
		if (pawnRenderNode_MilianApparel.apparel.RenderAsPack() && pawnRenderNode_MilianApparel.apparel.def.apparel.wornGraphicData != null)
		{
			Vector2 vector = pawnRenderNode_MilianApparel.apparel.def.apparel.wornGraphicData.BeltOffsetAt(parms.facing, BodyTypeDefOf.Female);
			result.x += vector.x;
			result.z += vector.y;
		}
		return result;
	}

	public override Vector3 ScaleFor(PawnRenderNode n, PawnDrawParms parms)
	{
		Vector3 result = base.ScaleFor(n, parms);
		PawnRenderNode_MilianApparel pawnRenderNode_MilianApparel = (PawnRenderNode_MilianApparel)n;
		if (pawnRenderNode_MilianApparel.apparel.RenderAsPack())
		{
			Vector2 vector = pawnRenderNode_MilianApparel.apparel.def.apparel.wornGraphicData.BeltScaleAt(parms.facing, BodyTypeDefOf.Female);
			result.x *= vector.x;
			result.z *= vector.y;
		}
		return result;
	}

	public override float LayerFor(PawnRenderNode node, PawnDrawParms parms)
	{
		PawnRenderNode_MilianApparel pawnRenderNode_MilianApparel = (PawnRenderNode_MilianApparel)node;
		if (pawnRenderNode_MilianApparel.apparel != null && pawnRenderNode_MilianApparel.apparel.RenderAsPack())
		{
			PawnDrawParms pawnDrawParms = parms;
			pawnDrawParms.facing = parms.facing.Opposite;
			pawnDrawParms.flipHead = false;
			return base.LayerFor(pawnRenderNode_MilianApparel, parms);
		}
		return base.LayerFor(pawnRenderNode_MilianApparel, parms);
	}
}
