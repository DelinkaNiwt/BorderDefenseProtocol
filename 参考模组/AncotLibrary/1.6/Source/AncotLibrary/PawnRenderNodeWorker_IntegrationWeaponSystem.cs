using Verse;

namespace AncotLibrary;

public class PawnRenderNodeWorker_IntegrationWeaponSystem : PawnRenderNodeWorker
{
	public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
	{
		if (!base.CanDrawNow(node, parms))
		{
			return false;
		}
		PawnRenderNode_IntegrationWeaponSystem pawnRenderNode_IntegrationWeaponSystem = node as PawnRenderNode_IntegrationWeaponSystem;
		PawnRenderNodeProperties_IntegrationWeaponSystem pawnRenderNodeProperties_IntegrationWeaponSystem = node.Props as PawnRenderNodeProperties_IntegrationWeaponSystem;
		pawnRenderNode_IntegrationWeaponSystem.SystemComp = pawnRenderNode_IntegrationWeaponSystem.apparel.TryGetComp<CompIntegrationWeaponSystem>();
		if (pawnRenderNode_IntegrationWeaponSystem.SystemComp != null && pawnRenderNode_IntegrationWeaponSystem.SystemComp.activate)
		{
			return true;
		}
		return false;
	}
}
