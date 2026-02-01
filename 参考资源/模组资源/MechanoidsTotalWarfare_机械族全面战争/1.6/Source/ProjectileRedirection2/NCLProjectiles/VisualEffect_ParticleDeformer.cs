using System;
using UnityEngine;

namespace NCLProjectiles;

public class VisualEffect_ParticleDeformer : VisualEffect_Particle
{
	protected Func<float, float> widthFunction;

	protected Func<float, float> lengthFunction;

	public VisualEffect_ParticleDeformer(EffectMapComponent parentComponent, EffectContext context)
		: base(parentComponent, context)
	{
	}

	protected override void PreInitialize(EffectContext context)
	{
		base.PreInitialize(context);
		widthFunction = AnimationUtility.GetFunctionByName(def.widthFunction);
		lengthFunction = AnimationUtility.GetFunctionByName(def.lengthFunction);
	}

	protected override void Initialize(EffectContext context)
	{
		base.Initialize(context);
	}

	protected float GetWidth(float value)
	{
		if (def.minWidth <= 0f)
		{
			return value;
		}
		return Mathf.Lerp(def.minWidth, 1f, value);
	}

	protected float GetLength(float value)
	{
		if (def.minLength <= 0f)
		{
			return value;
		}
		return Mathf.Lerp(def.minLength, 1f, value);
	}

	protected override void DrawInternal()
	{
		Vector3 s = drawSize * sizeFactor;
		if (widthFunction != null)
		{
			s.x *= GetWidth(widthFunction(progress));
		}
		if (lengthFunction != null)
		{
			s.z *= GetLength(lengthFunction(progress));
		}
		Matrix4x4 matrix = Matrix4x4.TRS(base.Position, rotation, s);
		DrawInternal(ref matrix);
	}
}
