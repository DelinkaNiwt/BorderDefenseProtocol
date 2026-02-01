using System;
using UnityEngine;
using Verse;

namespace NCLProjectiles;

public class VisualEffect_ParticleDrifter : VisualEffect_Particle
{
	protected Vector3 origin;

	protected Vector3 destination;

	protected Vector3 movementVector;

	protected float height;

	protected Func<float, float> pathingFunction;

	protected Func<float, float> heightFunction;

	protected AdditionalMotion additionalMotion;

	protected Vector3 MotionOffset
	{
		get
		{
			if (pathingFunction != null)
			{
				return movementVector * pathingFunction(progress);
			}
			return Vector3.zero;
		}
	}

	protected Vector3 HeightOffset
	{
		get
		{
			if (height == 0f)
			{
				return Vector3.zero;
			}
			return heightFunction(progress) * height * Vector3.forward;
		}
	}

	protected Vector3 AdditionalOffset => additionalMotion?.Resolve(progressTicks) ?? Vector3.zero;

	public VisualEffect_ParticleDrifter(EffectMapComponent parentComponent, EffectContext context)
		: base(parentComponent, context)
	{
		origin = context.position;
		origin.y = def.altitude.AltitudeFor(def.altitudeAdjustment);
		destination = context.destination + def.destinationDrawOffset;
		destination.y = def.altitude.AltitudeFor(def.altitudeAdjustment);
		movementVector = (destination - origin).Yto0();
		movementVector.y += 0.03658537f * (float)def.altitudeDrift * (def.syncAltitudeDrift ? base.DurationFactor : 1f);
		pathingFunction = AnimationUtility.GetFunctionByName(def.pathingFunction, AnimationUtility.Linear);
		height = def.height.RandomInRange;
		heightFunction = AnimationUtility.GetFunctionByName(def.heightFunction, AnimationUtility.Sine);
		additionalMotion = def.additionalMotion?.CreateInstance();
		if (def.inheritRotationFromOrbit)
		{
			rotation = ((movementVector == Vector3.zero) ? Quaternion.identity : Quaternion.LookRotation(movementVector));
			angle = rotation.eulerAngles.y;
		}
	}

	protected override bool CalculatePosition()
	{
		if (base.IsActive)
		{
			if (anchor != null)
			{
				Vector3 anchorPosition = VisualEffect_Particle.GetAnchorPosition(anchor);
				if (anchorPosition == Vector3.zero)
				{
					return false;
				}
				SetPosition(anchorPosition + positionOffset + MotionOffset + HeightOffset + AdditionalOffset, normalize: false);
			}
			else
			{
				SetPosition(origin + positionOffset + MotionOffset + HeightOffset + AdditionalOffset, normalize: false);
			}
		}
		return true;
	}
}
