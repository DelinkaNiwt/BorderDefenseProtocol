using UnityEngine;
using Verse;

namespace NCLProjectiles;

public class Projectile_ExplosiveStagedWithEffects : Projectile_ExplosiveWithEffects
{
	protected ProjectileStagingTracker staging;

	protected int calculatedRuntimeTicks = -1;

	protected int TicksSinceLaunch => calculatedRuntimeTicks - ticksToImpact;

	public override string Label
	{
		get
		{
			if (staging?.stageConfig != null)
			{
				return LabelNoCount + " (" + staging.stageConfig.label + ")";
			}
			return base.Label;
		}
	}

	public override void PostMake()
	{
		base.PostMake();
		staging = new ProjectileStagingTracker(this);
	}

	public override void SpawnSetup(Map map, bool respawningAfterLoad)
	{
		base.SpawnSetup(map, respawningAfterLoad);
		staging.PostSpawnSetup(map, respawningAfterLoad, effectsExtension, TicksSinceLaunch);
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Deep.Look(ref staging, "stagingTracker", this);
		Scribe_Values.Look(ref calculatedRuntimeTicks, "calculatedRuntimeTicks", 0);
	}

	protected override void TickInterval(int delta)
	{
		staging.PreTick(effectsExtension, effects, TicksSinceLaunch, activeTracking);
		Map map = base.Map;
		base.TickInterval(delta);
		staging.Tick(map, effectsExtension);
	}

	protected override void CalculateExactPosition()
	{
		ProjectileUtility.CalculateExactPosition(this, effects, staging, TicksSinceLaunch);
		cachedPositionTick = ticksToImpact;
	}

	protected override void CalculateExactRotation()
	{
		ProjectileUtility.CalculateExactRotation(this, effects, staging, effectsExtension, TicksSinceLaunch);
	}

	public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventFriendlyFire = false, Thing equipment = null, ThingDef targetCoverDef = null)
	{
		effects.PreLaunch(equipment, ref origin, usedTarget.Cell.ToVector3Shifted());
		BaseLaunch(launcher, origin, usedTarget, intendedTarget, hitFlags, preventFriendlyFire, equipment, targetCoverDef);
		if (effectsExtension != null && effectsExtension.activeTracking)
		{
			Thing thing = intendedTarget.Thing;
			if (thing != null && thing.Spawned)
			{
				activeTracking = true;
			}
		}
		calculatedRuntimeTicks = (ticksToImpact = staging.PostLaunch(effectsExtension, base.origin.Yto0(), destination.Yto0()));
		effects.parentDuration = ticksToImpact;
		effects.PostLaunch(base.origin, destination, calculatePositionImmediately: false);
	}
}
