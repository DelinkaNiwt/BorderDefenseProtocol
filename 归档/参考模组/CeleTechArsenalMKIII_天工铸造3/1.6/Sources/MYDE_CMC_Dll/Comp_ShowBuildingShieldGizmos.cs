using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MYDE_CMC_Dll;

[StaticConstructorOnStartup]
public class Comp_ShowBuildingShieldGizmos : ThingComp
{
	public CompProperties_ShowBuildingShieldGizmos PropsSpawner => (CompProperties_ShowBuildingShieldGizmos)props;

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		yield return new Gizmo_ProjectileInterceptorHitPoints
		{
			interceptor = parent.TryGetComp<CompProjectileInterceptor>()
		};
	}
}
