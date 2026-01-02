using Verse;

namespace AncotLibrary;

public class PawnRenderNodeProperties_IntegrationWeaponSystem : PawnRenderNodeProperties
{
	public PawnRenderNodeProperties_IntegrationWeaponSystem()
	{
		nodeClass = typeof(PawnRenderNode_IntegrationWeaponSystem);
		workerClass = typeof(PawnRenderNodeWorker_IntegrationWeaponSystem);
	}
}
