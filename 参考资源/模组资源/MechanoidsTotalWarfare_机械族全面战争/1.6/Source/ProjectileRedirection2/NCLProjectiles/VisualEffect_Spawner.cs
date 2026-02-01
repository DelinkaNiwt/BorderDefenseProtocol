namespace NCLProjectiles;

public class VisualEffect_Spawner : VisualEffect_Particle
{
	protected EffectContext originalContext;

	public VisualEffect_Spawner(EffectMapComponent parentComponent, EffectContext context)
		: base(parentComponent, context)
	{
		originalContext = context;
	}

	public override bool Tick()
	{
		if (base.Tick())
		{
			if (delay < 1 && def.subeffects != null)
			{
				foreach (EffectDef subeffect in def.subeffects)
				{
					if (subeffect.ShouldBeActive(progressTicks))
					{
						parentComponent.CreateEffect(originalContext.CreateSubEffectContext(subeffect, progressTicks));
					}
				}
			}
			return true;
		}
		return false;
	}
}
