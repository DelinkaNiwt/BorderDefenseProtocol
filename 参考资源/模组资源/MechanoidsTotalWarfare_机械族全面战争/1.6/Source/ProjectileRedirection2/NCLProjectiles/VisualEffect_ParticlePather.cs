using System;
using UnityEngine;
using Verse;

namespace NCLProjectiles;

public class VisualEffect_ParticlePather : VisualEffect_Particle
{
	protected Vector3 origin;

	protected Vector3 destination;

	protected Vector3 movementVector;

	protected float height;

	protected bool hasSubEffects;

	protected Func<float, float> heightFunction;

	protected PositionTracker tracker;

	public VisualEffect_ParticlePather(EffectMapComponent parentComponent, EffectContext context)
		: base(parentComponent, context)
	{
	}

	protected override void PreInitialize(EffectContext context)
	{
		base.PreInitialize(context);
		origin = context.origin;
		origin.y = def.altitude.AltitudeFor(def.altitudeAdjustment);
		destination = context.destination;
		destination.y = def.altitude.AltitudeFor(def.altitudeAdjustment);
		movementVector = (destination - origin).Yto0();
		movementVector.y += 0.03658537f * (float)def.altitudeDrift * (def.syncAltitudeDrift ? base.DurationFactor : 1f);
		height = context.def.height.RandomInRange;
		heightFunction = AnimationUtility.GetFunctionByName(context.def.heightFunction);
		if (parentComponent != null && !def.subeffects.NullOrEmpty())
		{
			hasSubEffects = true;
			tracker = new PositionTracker
			{
				previousVisualPosition = origin,
				currentVisualPosition = origin,
				currentVisualRotation = context.rotation,
				currentVisualAngle = context.angle
			};
		}
	}

	public override bool Tick()
	{
		if (base.Tick())
		{
			if (hasSubEffects && delay < 1)
			{
				tracker.Tick(base.Position);
				GenerateSubEffects();
			}
			return true;
		}
		return false;
	}

	protected override bool CalculatePosition()
	{
		Vector3 pos = origin + progress * movementVector;
		if (height != 0f)
		{
			pos.z += ((heightFunction == null) ? height : (heightFunction(progress) * height));
		}
		SetPosition(pos, normalize: false);
		return true;
	}

	protected virtual void GenerateSubEffects()
	{
		for (int i = 0; i < def.subeffects.Count; i++)
		{
			EffectDef effectDef = def.subeffects[i];
			if (effectDef.ShouldBeActive(progressTicks) && effectDef.CheckInterval(progressTicks))
			{
				parentComponent.CreateEffect(new EffectContext(parentComponent.map, effectDef)
				{
					anchor = null,
					destinationAnchor = null,
					position = tracker.currentVisualPosition,
					origin = tracker.currentVisualPosition,
					destination = tracker.previousVisualPosition,
					rotation = tracker.currentVisualRotation,
					angle = tracker.currentVisualAngle,
					parentTicksElapsed = progressTicks
				});
			}
		}
	}
}
