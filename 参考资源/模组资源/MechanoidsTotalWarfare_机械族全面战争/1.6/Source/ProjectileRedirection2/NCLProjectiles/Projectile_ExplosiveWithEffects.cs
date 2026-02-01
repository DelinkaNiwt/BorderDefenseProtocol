using UnityEngine;
using Verse;

namespace NCLProjectiles;

public class Projectile_ExplosiveWithEffects : Projectile_Explosive
{
	protected ModExtension_ProjectileEffects effectsExtension;

	protected ProjectileEffectTracker effects;

	protected int cachedPositionTick = -1;

	protected bool impacted;

	protected bool activeTracking;

	protected virtual Material ShadowMaterial => UIAssets.ProjectileShadowMaterial;

	protected override int MaxTickIntervalRate => 1;

	public override Vector3 DrawPos => effects.currentVisualPosition;

	public override Vector3 ExactPosition
	{
		get
		{
			if (cachedPositionTick != ticksToImpact)
			{
				CalculateExactPosition();
			}
			return effects.currentExactPosition;
		}
	}

	public override Quaternion ExactRotation => effects.currentVisualRotation;

	protected virtual float ArcHeightFactor => def.projectile.arcHeightFactor;

	public override void PostMake()
	{
		base.PostMake();
		effects = new ProjectileEffectTracker(this);
	}

	public override void SpawnSetup(Map map, bool respawningAfterLoad)
	{
		base.SpawnSetup(map, respawningAfterLoad);
		effectsExtension = def.GetModExtension<ModExtension_ProjectileEffects>();
		effects.PostSpawnSetup(map, respawningAfterLoad);
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Deep.Look(ref effects, "effectTracker", this);
		Scribe_Values.Look(ref activeTracking, "activeTracking", defaultValue: false);
	}

	public virtual void BaseTickInterval(int delta)
	{
		base.TickInterval(delta);
	}

	protected override void TickInterval(int delta)
	{
		if (activeTracking)
		{
			Thing thing = intendedTarget.Thing;
			if (thing != null && thing.Spawned)
			{
				destination = thing.DrawPos;
			}
		}
		effects.PreTick(origin, destination);
		CalculateExactPosition();
		Map map = base.Map;
		base.TickInterval(delta);
		CalculateExactRotation();
		effects.Tick(map, effectsExtension);
		effects.PostTick(delta);
	}

	protected override void DrawAt(Vector3 drawLoc, bool flip = false)
	{
		if (def.projectile.shadowSize > 0f && def.projectile.arcHeightFactor > 0f)
		{
			ProjectileUtility.DrawShadow(this, effects, ShadowMaterial);
		}
		DrawMainMesh();
		Comps_PostDraw();
	}

	protected override void Impact(Thing hitThing, bool blockedByShield = false)
	{
		Map map = base.Map;
		CalculateExactRotation();
		CalculateExactPosition();
		base.Impact(hitThing, blockedByShield);
		effects.Impact(map, hitThing, blockedByShield);
	}

	protected virtual void DrawMainMesh()
	{
		ProjectileUtility.DrawProjectileMesh(this, effects);
	}

	protected virtual void CalculateExactPosition()
	{
		ProjectileUtility.CalculateExactPosition(this, effects, origin, destination, base.DistanceCoveredFraction);
		cachedPositionTick = ticksToImpact;
	}

	protected virtual void CalculateExactRotation()
	{
		ProjectileUtility.CalculateExactRotation(this, effects, effectsExtension, origin, destination, base.DistanceCoveredFraction);
	}

	public void BaseLaunch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventFriendlyFire = false, Thing equipment = null, ThingDef targetCoverDef = null)
	{
		base.Launch(launcher, origin, usedTarget, intendedTarget, hitFlags, preventFriendlyFire, equipment, targetCoverDef);
	}

	public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventFriendlyFire = false, Thing equipment = null, ThingDef targetCoverDef = null)
	{
		effects.PreLaunch(equipment, ref origin, usedTarget.Cell.ToVector3Shifted());
		base.Launch(launcher, origin, usedTarget, intendedTarget, hitFlags, preventFriendlyFire, equipment, targetCoverDef);
		if (effectsExtension != null && effectsExtension.activeTracking)
		{
			Thing thing = intendedTarget.Thing;
			if (thing != null && thing.Spawned)
			{
				activeTracking = true;
			}
		}
		effects.parentDuration = ticksToImpact;
		effects.PostLaunch(base.origin, destination);
	}
}
