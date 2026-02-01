using RimWorld;
using Verse;

namespace NCL;

public class CompProperties_ExplosiveRefuelable : CompProperties
{
	public float minExplosionRadius = 5f;

	public float maxExplosionRadius = 50f;

	public float minClearRadius = 0f;

	public float maxClearRadius = 15f;

	public DamageDef damageDef;

	public float explosionDamageFactor = 30f;

	public bool requiresFuelForExplosion = true;

	public CompProperties_ExplosiveRefuelable()
	{
		compClass = typeof(CompExplosiveRefuelable);
	}

	public override void ResolveReferences(ThingDef parentDef)
	{
		base.ResolveReferences(parentDef);
		if (damageDef == null)
		{
			damageDef = DamageDefOf.Bomb;
		}
	}
}
