using Verse;

namespace AncotLibrary;

[StaticConstructorOnStartup]
public class CompProjectileInterceptorDestroyOnBreak : CompProjectileInterceptorWithHitPointsGizmo
{
	public override void CompTick()
	{
		if (currentHitPoints == 0)
		{
			parent.Destroy();
		}
		base.CompTick();
	}
}
