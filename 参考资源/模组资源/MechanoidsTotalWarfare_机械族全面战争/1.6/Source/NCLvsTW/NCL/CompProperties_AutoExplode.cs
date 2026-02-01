using Verse;

namespace NCL;

public class CompProperties_AutoExplode : CompProperties
{
	public int fuseTicks = 600;

	public float explosiveRadius = 3f;

	public DamageDef damageType;

	public int damAmount = -1;

	public float armorPenetration = -1f;

	public string customCountdownText;

	public CompProperties_AutoExplode()
	{
		compClass = typeof(Comp_AutoExplode);
	}
}
