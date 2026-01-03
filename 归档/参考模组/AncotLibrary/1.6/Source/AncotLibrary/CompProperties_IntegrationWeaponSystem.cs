using Verse;

namespace AncotLibrary;

public class CompProperties_IntegrationWeaponSystem : CompProperties_ApparelReloadable_Custom
{
	public int ticksConsumeChargeOnceWhenUse = 0;

	public EffecterDef activateEffect;

	public string gizmoLable;

	public string gizmoDesc;

	public string gizmoIconPath;

	public int ai_ActivateSystemTicksInterval = 180;

	public CompProperties_IntegrationWeaponSystem()
	{
		compClass = typeof(CompIntegrationWeaponSystem);
	}
}
