using Verse;

namespace AncotLibrary;

public class HediffComp_TakeDamage_DOT : HediffComp
{
	private HediffCompProperties_TakeDamage_DOT Props => (HediffCompProperties_TakeDamage_DOT)props;

	public override void CompPostTickInterval(ref float severityAdjustment, int delta)
	{
		base.CompPostTickInterval(ref severityAdjustment, delta);
		if (base.Pawn != null && base.Pawn.IsHashIntervalTick(Props.ticksBetweenDamage, delta) && !base.Pawn.Dead)
		{
			TakeDamage();
		}
	}

	public virtual void TakeDamage()
	{
		if (Props.damageDef != null)
		{
			DamageInfo dinfo = new DamageInfo(Props.damageDef, Props.damageAmountBase, Props.armorPenetrationBase);
			base.Pawn.TakeDamage(dinfo);
			parent.Severity += Props.severityPerTime;
		}
	}
}
