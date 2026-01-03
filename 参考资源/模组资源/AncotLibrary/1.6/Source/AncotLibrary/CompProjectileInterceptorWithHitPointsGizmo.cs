using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AncotLibrary;

[StaticConstructorOnStartup]
public class CompProjectileInterceptorWithHitPointsGizmo : CompProjectileInterceptor
{
	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		yield return new Gizmo_ProjectileInterceptorHitPoints
		{
			interceptor = parent.TryGetComp<CompProjectileInterceptor>()
		};
	}
}
