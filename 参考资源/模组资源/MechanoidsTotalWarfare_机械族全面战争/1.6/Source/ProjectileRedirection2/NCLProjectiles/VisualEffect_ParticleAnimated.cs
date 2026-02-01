using UnityEngine;
using Verse;

namespace NCLProjectiles;

public class VisualEffect_ParticleAnimated : VisualEffect_Particle
{
	public VisualEffect_ParticleAnimated(EffectMapComponent parentComponent, EffectContext context)
		: base(parentComponent, context)
	{
	}

	protected override void PreInitialize(EffectContext context)
	{
		base.PreInitialize(context);
		if (def.opacityFunction.NullOrEmpty())
		{
			opacityFunction = null;
		}
	}

	protected override void Initialize(EffectContext context)
	{
		base.Initialize(context);
		CalculateMaterial();
	}

	public override bool Tick()
	{
		return base.Tick() && CalculateMaterial();
	}

	protected virtual bool CalculateMaterial()
	{
		if (def.randomizeMaterial && progressTicks % def.materialInterval == 0)
		{
			material = def.Material;
			if (def.randomizeAngle)
			{
				angle = originalAngle + def.rotationOffset.RandomInRange;
				rotation = Quaternion.Euler(0f, angle, 0f);
			}
		}
		else
		{
			material = def.MaterialForProgress(progress);
		}
		return material != null;
	}
}
