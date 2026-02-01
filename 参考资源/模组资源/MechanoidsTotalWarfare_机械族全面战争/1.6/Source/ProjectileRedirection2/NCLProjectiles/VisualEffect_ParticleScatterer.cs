using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace NCLProjectiles;

public class VisualEffect_ParticleScatterer : VisualEffect
{
	protected List<IntVec3> cells;

	protected int ticksUntilNextInterval;

	public VisualEffect_ParticleScatterer(EffectMapComponent parentComponent, EffectContext context)
		: base(parentComponent, context)
	{
	}

	protected override void PreInitialize(EffectContext context)
	{
		base.PreInitialize(context);
		cells = GenRadial.RadialCellsAround(context.position.ToIntVec3(), def.radius, useCenter: true).ToList();
		cells.Shuffle();
	}

	protected override void Initialize(EffectContext context)
	{
	}

	public override bool Tick()
	{
		if (!base.Tick())
		{
			return false;
		}
		if (delay > 0)
		{
			return true;
		}
		if (ticksUntilNextInterval < 1)
		{
			for (int i = 0; i < def.count; i++)
			{
				if (cells.NullOrEmpty())
				{
					return false;
				}
				IntVec3 intVec = cells.Pop();
				if (intVec.IsValid && intVec.InBounds(parentComponent.map))
				{
					for (int j = 0; j < def.subeffects.Count; j++)
					{
						SpawnEffects(intVec);
					}
				}
			}
			ticksUntilNextInterval = def.interval;
		}
		ticksUntilNextInterval--;
		return true;
	}

	protected virtual void SpawnEffects(IntVec3 cell)
	{
		Vector3 destination = cell.ToVector3Shifted();
		for (int i = 0; i < def.subeffects.Count; i++)
		{
			if (def.distance.max > 0f)
			{
				destination += Quaternion.Euler(0f, def.rotationOffset.RandomInRange, 0f) * new Vector3(0f, 0f, def.distance.RandomInRange);
			}
			parentComponent.CreateEffect(new EffectContext(parentComponent.map, def.subeffects[i])
			{
				anchor = null,
				destinationAnchor = null,
				position = destination,
				origin = base.Position,
				destination = destination,
				parentTicksElapsed = progressTicks
			});
		}
	}

	protected override void DrawInternal()
	{
	}
}
