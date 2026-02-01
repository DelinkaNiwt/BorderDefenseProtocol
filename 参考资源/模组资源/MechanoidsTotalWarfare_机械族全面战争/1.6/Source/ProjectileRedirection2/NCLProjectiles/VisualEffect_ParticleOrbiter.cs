using System;
using UnityEngine;

namespace NCLProjectiles;

public class VisualEffect_ParticleOrbiter : VisualEffect_Particle
{
	protected Vector3 centerpoint;

	protected float orbitRadius;

	protected float orbitAngle;

	protected float orbitRate;

	protected Func<float, float> radiusFunction;

	public VisualEffect_ParticleOrbiter(EffectMapComponent parentComponent, EffectContext context)
		: base(parentComponent, context)
	{
	}

	protected override void PreInitialize(EffectContext context)
	{
		base.PreInitialize(context);
		centerpoint = context.position;
		orbitRadius = def.radius;
		orbitAngle = context.orbitAngle;
		orbitRate = def.orbitRate.RandomInRange;
		radiusFunction = AnimationUtility.GetFunctionByName(def.radiusFunction);
	}

	protected override bool CalculatePosition()
	{
		if (!base.IsActive)
		{
			return true;
		}
		if (orbitRate != 0f)
		{
			orbitAngle = (orbitAngle + orbitRate) % 360f;
		}
		if (!CalculateRadius())
		{
			return false;
		}
		Vector3 vector = CalculateOrbitalOffset();
		if (anchor == null)
		{
			SetPosition(centerpoint + vector);
		}
		else
		{
			Vector3 anchorPosition = VisualEffect_Particle.GetAnchorPosition(anchor);
			if (anchorPosition == Vector3.zero)
			{
				return false;
			}
			SetPosition(anchorPosition + positionOffset + vector);
		}
		return true;
	}

	protected virtual bool CalculateRadius()
	{
		if (radiusFunction != null)
		{
			orbitRadius = radiusFunction(progress);
		}
		return true;
	}

	protected virtual Vector3 CalculateOrbitalOffset()
	{
		float f = (float)Math.PI * (90f - orbitAngle) / 180f;
		return new Vector3(orbitRadius * Mathf.Cos(f), 0f, orbitRadius * Mathf.Sin(f));
	}
}
