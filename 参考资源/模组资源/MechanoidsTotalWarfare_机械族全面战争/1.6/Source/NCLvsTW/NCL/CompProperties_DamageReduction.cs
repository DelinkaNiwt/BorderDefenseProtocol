using Verse;

namespace NCL;

public class CompProperties_DamageReduction : CompProperties
{
	public float minHealthPercent = 0.5f;

	public float minDamageFactor = 0f;

	public CompProperties_DamageReduction()
	{
		compClass = typeof(CompDamageReduction);
	}
}
