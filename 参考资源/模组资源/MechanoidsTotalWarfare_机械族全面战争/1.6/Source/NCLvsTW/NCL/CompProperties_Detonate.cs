using RimWorld;
using Verse;

namespace NCL;

public class CompProperties_Detonate : CompProperties_AbilityEffect
{
	public float radius;

	public DamageDef damageType;

	public int damageAmount = -1;

	public float damagePenetration = -1f;

	public SoundDef soundCreated = null;

	public ThingDef thingCreated = null;

	public float thingCreatedChance = 0f;

	public float chanceToStartFire = 0f;

	public bool damageUser = true;

	public bool killUser = false;

	public CompProperties_Detonate()
	{
		compClass = typeof(CompDetonate);
	}
}
