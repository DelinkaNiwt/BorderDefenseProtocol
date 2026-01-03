using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompProperties_SelfDetonation : CompProperties
{
	public float radius = 2f;

	public int damageAmount = 50;

	public float armorPenetration = 1f;

	public DamageDef damageDef = DamageDefOf.Bomb;

	public float angle = 0f;

	public CompProperties_SelfDetonation()
	{
		compClass = typeof(CompSelfDetonation);
	}
}
