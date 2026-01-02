using AncotLibrary;
using Verse;

namespace Milira;

public class PawnRenderNodeWorker_BodyPartFlight : PawnRenderNodeWorker_BodyPart
{
	public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
	{
		if (!parms.pawn.Flying)
		{
			Pawn_DrawTracker drawer = parms.pawn.Drawer;
			if (drawer == null || drawer.renderer?.CurAnimation?.defName.StartsWith("Milira_Fly") != true)
			{
				return ((PawnRenderNodeWorker_BodyPart)this).CanDrawNow(node, parms);
			}
		}
		return false;
	}
}
