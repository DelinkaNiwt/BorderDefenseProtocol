using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompProperties_AbilityDoExplosion : CompProperties_AbilityEffect
{
	public float radius = 2f;

	public int damageAmount = 50;

	public float armorPenetration = 1f;

	public DamageDef damageDef = DamageDefOf.Bomb;

	public ThingDef postExplosionSpawnThingDef;

	public float postExplosionSpawnChance;

	public int postExplosionSpawnThingCount = 1;

	public bool applyDamageToExplosionCellsNeighbors;

	public ThingDef preExplosionSpawnThingDef;

	public float preExplosionSpawnChance;

	public int preExplosionSpawnThingCount = 1;

	public float chanceToStartFire;

	public bool damageFalloff;

	public bool explodeOnKilled;

	public bool explodeOnDestroyed;

	public GasType? postExplosionGasType;

	public bool doVisualEffects = true;

	public bool doSoundEffects = true;

	public float propagationSpeed = 1f;

	public float explosiveExpandPerStackcount;

	public float explosiveExpandPerFuel;

	public EffecterDef explosionEffect;

	public SoundDef explosionSound;

	public bool targetOnCaster;

	public EffecterDef warmupEffect;

	public int warmupEffectMaintainTicks = 17;

	public CompProperties_AbilityDoExplosion()
	{
		compClass = typeof(CompAbilityDoExplosion);
	}
}
