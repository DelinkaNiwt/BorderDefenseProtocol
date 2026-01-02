using Verse;

namespace Milira;

public class PawnRenderNodeWorker_Milian : PawnRenderNodeWorker
{
	public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
	{
		if (!base.CanDrawNow(node, parms))
		{
			return false;
		}
		return MilianUtility.IsMilian(parms.pawn);
	}
}
