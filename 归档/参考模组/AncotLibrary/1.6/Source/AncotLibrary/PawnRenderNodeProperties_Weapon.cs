using Verse;

namespace AncotLibrary;

public class PawnRenderNodeProperties_Weapon : PawnRenderNodeProperties
{
	[NoTranslate]
	public string texPath_Undrafted;

	public bool colored = true;

	public PawnRenderNodeProperties_Weapon()
	{
		nodeClass = typeof(PawnRenderNode_Weapon);
		workerClass = typeof(PawnRenderNodeWorker_Weapon);
	}
}
