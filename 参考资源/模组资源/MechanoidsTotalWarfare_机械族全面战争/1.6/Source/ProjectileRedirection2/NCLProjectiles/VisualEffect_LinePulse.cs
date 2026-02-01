using System;
using UnityEngine;
using Verse;

namespace NCLProjectiles;

public class VisualEffect_LinePulse : VisualEffect_Line
{
	protected Vector3 delta;

	protected Vector3 pulsePosition;

	protected float pulseLength;

	protected float pulseProgress;

	protected Func<float, float> pulseFunction;

	public VisualEffect_LinePulse(EffectMapComponent parentComponent, EffectContext context)
		: base(parentComponent, context)
	{
	}

	protected override void PreInitialize(EffectContext context)
	{
		base.PreInitialize(context);
		delta = (destination - position).Yto0();
		pulseFunction = AnimationUtility.GetFunctionByName(def.pathingFunction, AnimationUtility.Linear);
	}

	protected override void Initialize(EffectContext context)
	{
		base.Initialize(context);
		CalculatePulse();
	}

	public override bool Tick()
	{
		return base.Tick() && CalculatePulse();
	}

	protected virtual bool CalculatePulse()
	{
		if (anchor != null || destinationAnchor != null)
		{
			delta = (destination - position).Yto0();
			rotation = ((delta == Vector3.zero) ? Quaternion.identity : Quaternion.LookRotation(delta));
			angle = rotation.eulerAngles.y;
		}
		pulseProgress = pulseFunction(base.Progress);
		pulseLength = 2f * ((pulseProgress < 0.5f) ? pulseProgress : (1f - pulseProgress));
		pulsePosition = position + pulseProgress * delta;
		pulsePosition.y = def.altitude.AltitudeFor(def.altitudeAdjustment);
		return true;
	}

	protected override void DrawInternal()
	{
		if (pulseLength > 0f)
		{
			Vector3 s = new Vector3(sizeFactor, 1f, pulseLength * delta.MagnitudeHorizontal());
			Matrix4x4 matrix = default(Matrix4x4);
			matrix.SetTRS(pulsePosition, rotation, s);
			Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
		}
	}
}
