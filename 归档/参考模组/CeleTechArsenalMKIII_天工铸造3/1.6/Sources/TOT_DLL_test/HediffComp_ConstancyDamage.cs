using Verse;

namespace TOT_DLL_test;

public class HediffComp_ConstancyDamage : HediffComp
{
	private int DamageTick;

	public HediffCompProperties_ConstancyDamage Props => (HediffCompProperties_ConstancyDamage)props;

	public override void CompPostTick(ref float severityAdjustment)
	{
		DamageTick++;
		if (DamageTick >= Props.DamageTickMax)
		{
			DamageInfo dinfo = new DamageInfo(Props.DamageDef, Props.DamageNum, Props.DamageArmorPenetration, -1f, base.Pawn);
			base.Pawn.TakeDamage(dinfo);
			DamageTick = 0;
		}
	}
}
