using Verse;

namespace NCLWorm;

public class CompProperties_DamageAbsorbedAndBoom : CompProperties
{
	public int AbsorbedTime = 24;

	public int BoomTime = 60;

	public int radius = 6;

	public string damageDefName = "Bomb";

	public int amount = 80;

	public float armorPenetration = 1f;

	[Unsaved(false)]
	public DamageDef damageDef;

	public CompProperties_DamageAbsorbedAndBoom()
	{
		compClass = typeof(Comp_DamageAbsorbedAndBoom);
	}
}
