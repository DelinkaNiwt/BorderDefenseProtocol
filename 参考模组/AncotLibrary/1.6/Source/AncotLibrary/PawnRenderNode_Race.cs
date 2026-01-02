using Verse;

namespace AncotLibrary;

public class PawnRenderNode_Race : PawnRenderNode
{
	public bool canDrawNow = true;

	public PawnRenderNode_Race(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
		: base(pawn, props, tree)
	{
		PawnRenderNodeProperties_Race pawnRenderNodeProperties_Race = props as PawnRenderNodeProperties_Race;
		canDrawNow = pawnRenderNodeProperties_Race.races.Contains(pawn.def.defName);
	}
}
