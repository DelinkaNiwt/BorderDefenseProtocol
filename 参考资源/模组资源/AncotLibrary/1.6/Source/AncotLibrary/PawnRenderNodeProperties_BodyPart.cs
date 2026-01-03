using Verse;

namespace AncotLibrary;

public class PawnRenderNodeProperties_BodyPart : PawnRenderNodeProperties
{
	public BodyPartDef bodyPart;

	public string bodyPartLabel;

	public bool drawWithoutPart;

	public PawnRenderNodeProperties_BodyPart()
	{
		nodeClass = typeof(PawnRenderNode_BodyPart);
		workerClass = typeof(PawnRenderNodeWorker_BodyPart);
	}
}
