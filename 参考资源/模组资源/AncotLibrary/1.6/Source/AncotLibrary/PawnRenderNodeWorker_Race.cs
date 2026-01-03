using Verse;

namespace AncotLibrary;

public class PawnRenderNodeWorker_Race : PawnRenderNodeWorker
{
	public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
	{
		if (!base.CanDrawNow(node, parms))
		{
			return false;
		}
		PawnRenderNode_Race pawnRenderNode_Race = node as PawnRenderNode_Race;
		PawnRenderNodeProperties_Race pawnRenderNodeProperties_Race = node.Props as PawnRenderNodeProperties_Race;
		return pawnRenderNode_Race.canDrawNow;
	}
}
