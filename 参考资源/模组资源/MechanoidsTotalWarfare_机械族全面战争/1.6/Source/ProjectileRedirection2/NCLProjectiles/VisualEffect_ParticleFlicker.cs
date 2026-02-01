using UnityEngine;

namespace NCLProjectiles;

public class VisualEffect_ParticleFlicker : VisualEffect_Particle
{
	public VisualEffect_ParticleFlicker(EffectMapComponent parentComponent, EffectContext context)
		: base(parentComponent, context)
	{
		sizeFactor = (baseSize = def.size);
	}

	public override bool Tick()
	{
		if (base.Tick())
		{
			material = def.Material;
			return true;
		}
		return false;
	}

	protected override bool CalculateSize()
	{
		if (base.CalculateSize())
		{
			sizeFactor *= def.sizeRange.RandomInRange;
			return true;
		}
		return false;
	}

	protected override bool CalculateRotation()
	{
		if (base.IsActive)
		{
			float randomInRange = def.rotationOffset.RandomInRange;
			if (randomInRange != 0f)
			{
				angle = originalAngle + randomInRange;
				rotation = Quaternion.Euler(0f, angle, 0f);
			}
			else
			{
				base.CalculateRotation();
			}
		}
		return true;
	}
}
