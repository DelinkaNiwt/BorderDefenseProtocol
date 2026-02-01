using Verse;

namespace NCL;

public class HediffCompProperties_ExplodeOnRemove : HediffCompProperties
{
	public string damageDefName;

	public DamageDef damageDef;

	public int damageAmount = 50;

	public float explosionRadius = 3f;

	public bool affectFriendly = false;

	public HediffCompProperties_ExplodeOnRemove()
	{
		compClass = typeof(HediffComp_ExplodeOnRemove);
	}
}
