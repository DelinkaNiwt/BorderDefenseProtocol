using Verse;

namespace MYDE_CMC_Dll;

public class HediffCompProperties_ConstancyDamage : HediffCompProperties
{
	public int DamageTickMax = 60;

	public DamageDef DamageDef;

	public int DamageNum = 1;

	public float DamageArmorPenetration = 0f;

	public HediffCompProperties_ConstancyDamage()
	{
		compClass = typeof(HediffComp_ConstancyDamage);
	}
}
