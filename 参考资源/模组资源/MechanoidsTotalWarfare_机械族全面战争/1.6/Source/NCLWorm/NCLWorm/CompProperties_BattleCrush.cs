using Verse;

namespace NCLWorm;

public class CompProperties_BattleCrush : CompProperties
{
	public EffecterDef battleEffect;

	public int damageInterval = 60;

	public float enemyScanRadius = 50f;

	public int damageRadius = 2;

	public float damageFactor = 0.1f;

	public CompProperties_BattleCrush()
	{
		compClass = typeof(Comp_BattleCrush);
	}
}
