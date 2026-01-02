using Verse;

namespace AncotLibrary;

public class PawnRenderNodeWorker_BodyPart : PawnRenderNodeWorker
{
	public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
	{
		if (!base.CanDrawNow(node, parms))
		{
			return false;
		}
		PawnRenderNode_BodyPart pawnRenderNode_BodyPart = node as PawnRenderNode_BodyPart;
		PawnRenderNodeProperties_BodyPart pawnRenderNodeProperties_BodyPart = node.Props as PawnRenderNodeProperties_BodyPart;
		return pawnRenderNodeProperties_BodyPart.drawWithoutPart || pawnRenderNode_BodyPart.canDrawNow;
	}
}
