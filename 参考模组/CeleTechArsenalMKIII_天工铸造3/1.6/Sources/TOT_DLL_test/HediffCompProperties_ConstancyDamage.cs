using Verse;

namespace TOT_DLL_test;

public class HediffCompProperties_ConstancyDamage : HediffCompProperties
{
	public int DamageTickMax;

	public DamageDef DamageDef;

	public int DamageNum;

	public float DamageArmorPenetration;

	public HediffCompProperties_ConstancyDamage()
	{
		compClass = typeof(HediffComp_ConstancyDamage);
	}
}
