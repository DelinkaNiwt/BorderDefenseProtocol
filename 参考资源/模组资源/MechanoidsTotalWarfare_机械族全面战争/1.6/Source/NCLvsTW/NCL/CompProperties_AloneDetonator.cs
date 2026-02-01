using Verse;

namespace NCL;

public class CompProperties_AloneDetonator : CompProperties
{
	public int fuseTicks = 600;

	public float explosiveRadius = 3f;

	public DamageDef damageType;

	public int damAmount = -1;

	public float armorPenetration = -1f;

	public string customCountdownText;

	public int checkInterval = 60;

	public bool includeDownedPawns = false;

	public bool spawnExplosionEffect = false;

	public CompProperties_AloneDetonator()
	{
		compClass = typeof(Comp_AloneDetonator);
	}
}
