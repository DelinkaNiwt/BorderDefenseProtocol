using System;
using UnityEngine;
using Verse;

namespace NCLProjectiles;

public class VisualEffect_TrailerOrbiter : VisualEffect_Particle
{
	protected float orbitRadius;

	protected float orbitAngle;

	protected float orbitRate;

	protected Func<float, float> radiusFunction;

	protected float height;

	protected Func<float, float> heightFunction;

	protected int trailCount;

	protected PositionTracker[] positionTrackers;

	public VisualEffect_TrailerOrbiter(EffectMapComponent parentComponent, EffectContext context)
		: base(parentComponent, context)
	{
	}

	protected override void PreInitialize(EffectContext context)
	{
		base.PreInitialize(context);
		orbitRadius = def.radius;
		orbitAngle = (def.applyRotationToOrbit ? angle : 0f) + def.orbitOffset.RandomInRange;
		orbitRate = def.orbitRate.RandomInRange;
		radiusFunction = AnimationUtility.GetFunctionByName(def.radiusFunction);
		height = def.height.RandomInRange;
		heightFunction = AnimationUtility.GetFunctionByName(def.heightFunction);
		trailCount = Mathf.Max(1, def.count);
		positionTrackers = new PositionTracker[trailCount];
		for (int i = 0; i < trailCount; i++)
		{
			positionTrackers[i] = new PositionTracker();
		}
	}

	public override bool Tick()
	{
		if (base.Tick() && CalculateRadius())
		{
			GenerateSubEffects();
			return true;
		}
		return false;
	}

	protected virtual void GenerateSubEffects()
	{
		if (orbitRate != 0f)
		{
			orbitAngle = (orbitAngle + orbitRate) % 360f;
		}
		if (parentComponent == null)
		{
			return;
		}
		Vector3 vector = base.Position;
		if (heightFunction != null && height != 0f)
		{
			vector.z += height * heightFunction(progress);
		}
		for (int i = 0; i < trailCount; i++)
		{
			PositionTracker positionTracker = positionTrackers[i];
			positionTracker.Tick(vector + CalculateOrbitalOffset(i));
			foreach (EffectDef subeffect in def.subeffects)
			{
				if (subeffect.ShouldBeActive(progressTicks))
				{
					parentComponent.CreateEffect(new EffectContext(parentComponent.map, subeffect)
					{
						anchor = null,
						destinationAnchor = null,
						position = positionTracker.currentVisualPosition,
						origin = positionTracker.currentVisualPosition,
						destination = positionTracker.previousVisualPosition,
						rotation = positionTracker.currentVisualRotation,
						angle = positionTracker.currentVisualAngle,
						parentTicksElapsed = progressTicks
					});
				}
			}
		}
	}

	protected virtual bool CalculateRadius()
	{
		if (radiusFunction != null)
		{
			float num = radiusFunction(base.RawProgress);
			orbitRadius = ((def.minRadius > 0f) ? Mathf.Lerp(def.minRadius, def.radius, num) : (def.radius * num));
		}
		return true;
	}

	protected virtual Vector3 CalculateOrbitalOffset(int index)
	{
		float num = orbitAngle + (float)index * (360f / (float)trailCount);
		float f = (float)Math.PI * (90f - num) / 180f;
		return new Vector3(orbitRadius * Mathf.Cos(f), 0f, orbitRadius * Mathf.Sin(f));
	}

	protected override void DrawInternal()
	{
		if (!(material != null))
		{
			return;
		}
		for (int i = 0; i < trailCount; i++)
		{
			PositionTracker positionTracker = positionTrackers[i];
			if (positionTracker != null && positionTracker.currentVisualPosition != Vector3.zero)
			{
				Vector3 currentVisualPosition = positionTracker.currentVisualPosition;
				currentVisualPosition.y = def.altitude.AltitudeFor();
				Matrix4x4 matrix = Matrix4x4.TRS(currentVisualPosition, def.neverDrawRotated ? Quaternion.identity : positionTracker.currentVisualRotation, drawSize * sizeFactor);
				DrawInternal(ref matrix);
			}
		}
	}
}
