using Verse;

namespace AncotLibrary;

public class PawnRenderNode_BodyPart : PawnRenderNode
{
	public bool canDrawNow = true;

	public PawnRenderNode_BodyPart(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
		: base(pawn, props, tree)
	{
		PawnRenderNodeProperties_BodyPart pawnRenderNodeProperties_BodyPart = props as PawnRenderNodeProperties_BodyPart;
		canDrawNow = AncotUtility.HasNamedBodyPart(pawnRenderNodeProperties_BodyPart.bodyPart, pawnRenderNodeProperties_BodyPart.bodyPartLabel, pawn);
	}
}
