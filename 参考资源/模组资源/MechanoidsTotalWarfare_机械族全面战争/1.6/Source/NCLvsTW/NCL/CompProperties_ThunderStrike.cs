using RimWorld;
using Verse;

namespace NCL;

public class CompProperties_ThunderStrike : CompProperties_AbilityEffect
{
	public int lightningCount = 3;

	public float radius = 3f;

	public int damageAmount = 40;

	public DamageDef damageType;

	public ThingDef postEffectFilth;

	public CompProperties_ThunderStrike()
	{
		compClass = typeof(CompAbilityEffect_ThunderStrike);
	}
}
