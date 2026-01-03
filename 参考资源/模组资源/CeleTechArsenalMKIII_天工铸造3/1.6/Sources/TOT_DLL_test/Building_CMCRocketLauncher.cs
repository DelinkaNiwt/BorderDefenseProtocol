using Verse;

namespace TOT_DLL_test;

[StaticConstructorOnStartup]
public class Building_CMCRocketLauncher : Building_CMCTurretGun
{
	public override bool CanSetForcedTarget => true;

	protected override void Tick()
	{
		base.Tick();
		if (forcedTarget.IsValid && !CanSetForcedTarget)
		{
			ResetForcedTarget();
		}
		if (!base.CanToggleHoldFire)
		{
			holdFire = false;
		}
		if (forcedTarget.ThingDestroyed)
		{
			ResetForcedTarget();
		}
		if (base.Active && (mannableComp == null || mannableComp.MannedNow) && !base.IsStunned && base.Spawned)
		{
			base.GunCompEq.verbTracker.VerbsTick();
			if (AttackVerb.state == VerbState.Bursting)
			{
				return;
			}
			burstActivated = false;
			if (base.WarmingUp && turrettop.CurRotation == turrettop.DestRotation)
			{
				burstWarmupTicksLeft--;
				if (burstWarmupTicksLeft == 0)
				{
					BeginBurst();
				}
			}
			else
			{
				if (burstCooldownTicksLeft > 0)
				{
					burstCooldownTicksLeft--;
				}
				if (burstCooldownTicksLeft <= 0 && this.IsHashIntervalTick(10))
				{
					TryStartShootSomething(canBeginBurstImmediately: true);
				}
			}
			turrettop.TurretTopTick();
		}
		else
		{
			ResetCurrentTarget();
		}
	}
}
