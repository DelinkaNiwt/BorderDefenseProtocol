using RimWorld;
using Verse;

namespace AncotLibrary;

public class HediffComp_RestoreProjectileInterceptor : HediffComp
{
	private CompProjectileInterceptor compProjectileInterceptor;

	private HediffCompProperties_RestoreProjectileInterceptor Props => (HediffCompProperties_RestoreProjectileInterceptor)props;

	private CompProjectileInterceptor CompProjectileInterceptor => compProjectileInterceptor ?? (compProjectileInterceptor = base.Pawn.TryGetComp<CompProjectileInterceptor>());

	public override void CompPostTickInterval(ref float severityAdjustment, int delta)
	{
		base.CompPostTickInterval(ref severityAdjustment, delta);
		if (base.Pawn.IsHashIntervalTick(20, delta) && !(parent.Severity < Props.severityThreshold) && CompProjectileInterceptor.Active && (float)CompProjectileInterceptor.currentHitPoints / (float)CompProjectileInterceptor.HitPointsMax < Props.restoreThreshold)
		{
			CompProjectileInterceptor.currentHitPoints += (int)((float)CompProjectileInterceptor.HitPointsMax * Props.restorePct);
			parent.Severity -= Props.severityCost;
			if (Props.effecter != null)
			{
				Effecter effecter = Props.effecter.Spawn();
				effecter.Trigger(new TargetInfo(base.Pawn.Position, base.Pawn.Map), new TargetInfo(base.Pawn.Position, base.Pawn.Map));
				effecter.Cleanup();
			}
		}
	}
}
