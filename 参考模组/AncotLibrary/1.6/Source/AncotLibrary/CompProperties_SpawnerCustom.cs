using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompProperties_SpawnerCustom : CompProperties
{
	public ThingDef thingToSpawn;

	public IntRange spawnCountRange = new IntRange(1, 10);

	public IntRange spawnIntervalRange = new IntRange(100, 100);

	public int spawnMaxAdjacent = -1;

	public bool spawnForbidden;

	public bool requiresPower;

	public bool requiresFuel;

	public bool writeTimeLeftToSpawn;

	public bool showMessageIfOwned;

	public string saveKeysPrefix;

	public bool inheritFaction;

	public bool explodeWhileSpawn = false;

	public DamageDef explodeDamageDef = DamageDefOf.Bomb;

	public IntRange explosionDamageRange = new IntRange(10, 20);

	public CompProperties_SpawnerCustom()
	{
		compClass = typeof(CompSpawnerCustom);
	}
}
