using System.Collections.Generic;
using Verse;

namespace NCLProjectiles;

public class VisualEffect_Sequencer : VisualEffect_Particle
{
	protected EffectContext originalContext;

	protected List<EffectDef> subeffects;

	protected int index;

	public VisualEffect_Sequencer(EffectMapComponent parentComponent, EffectContext context)
		: base(parentComponent, context)
	{
		originalContext = context;
		if (def.subeffects != null)
		{
			subeffects = new List<EffectDef>();
			subeffects.AddRange(def.subeffects);
			if (def.randomize)
			{
				subeffects.Shuffle();
			}
		}
	}

	public override bool Tick()
	{
		if (base.Tick())
		{
			if (delay < 1 && subeffects != null && def.CheckInterval(progressTicks))
			{
				if (index >= subeffects.Count)
				{
					index = 0;
				}
				parentComponent?.CreateEffect(originalContext.CreateSubEffectContext(subeffects[index], progressTicks));
				index++;
			}
			return true;
		}
		return false;
	}
}
