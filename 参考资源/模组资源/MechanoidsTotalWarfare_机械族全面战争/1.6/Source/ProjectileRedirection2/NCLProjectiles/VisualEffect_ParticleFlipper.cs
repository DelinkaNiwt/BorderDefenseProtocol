using System;
using UnityEngine;

namespace NCLProjectiles;

public class VisualEffect_ParticleFlipper : VisualEffect_Particle
{
	protected Material frontMaterial;

	protected Material backMaterial;

	protected float flipRate;

	protected float flipAngle;

	protected Func<float, float> flipFunction;

	public VisualEffect_ParticleFlipper(EffectMapComponent parentComponent, EffectContext context)
		: base(parentComponent, context)
	{
		frontMaterial = material;
		backMaterial = def.material ?? material;
		flipRate = def.flipRate.RandomInRange;
		flipAngle = def.flipOffset.RandomInRange % 360f;
		flipFunction = AnimationUtility.GetFunctionByName(def.flipFunction, AnimationUtility.Sine);
	}

	protected override bool CalculateSize()
	{
		if (base.CalculateSize())
		{
			if (def.flipStopsAt < 0 || progressTicks <= def.flipStopsAt)
			{
				flipAngle += flipRate;
				flipAngle %= 360f;
			}
			return true;
		}
		return false;
	}

	protected override void DrawInternal()
	{
		material = ((flipAngle < 180f) ? frontMaterial : backMaterial);
		if (flipFunction != null)
		{
			float num = flipFunction(flipAngle % 180f / 180f);
			if (num > 0f)
			{
				Vector3 s = drawSize * sizeFactor;
				s.x *= num;
				Matrix4x4 matrix = Matrix4x4.TRS(base.Position, rotation, s);
				DrawInternal(ref matrix);
			}
		}
		else
		{
			base.DrawInternal();
		}
	}
}
