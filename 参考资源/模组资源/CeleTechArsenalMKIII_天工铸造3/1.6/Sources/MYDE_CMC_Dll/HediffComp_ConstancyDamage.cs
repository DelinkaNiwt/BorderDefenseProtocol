using Verse;

namespace MYDE_CMC_Dll;

public class HediffComp_ConstancyDamage : HediffComp
{
	private int DamageTick = 0;

	public HediffCompProperties_ConstancyDamage Props => (HediffCompProperties_ConstancyDamage)props;

	public override void CompPostTick(ref float severityAdjustment)
	{
		DamageTick++;
		if (DamageTick >= Props.DamageTickMax)
		{
			TakeDamage();
			DamageTick = 0;
		}
	}

	public virtual void TakeDamage()
	{
		if (Props.DamageDef != null)
		{
			DamageInfo dinfo = new DamageInfo(Props.DamageDef, Props.DamageNum, Props.DamageArmorPenetration);
			base.Pawn.TakeDamage(dinfo);
		}
	}
}
