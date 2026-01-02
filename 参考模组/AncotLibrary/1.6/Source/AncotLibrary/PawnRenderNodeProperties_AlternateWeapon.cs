using Verse;

namespace AncotLibrary;

public class PawnRenderNodeProperties_AlternateWeapon : PawnRenderNodeProperties
{
	public PawnRenderNodeProperties_AlternateWeapon()
	{
		nodeClass = typeof(PawnRenderNode_AlternateWeapon);
		workerClass = typeof(PawnRenderNodeWorker_AlternateWeapon);
	}
}
