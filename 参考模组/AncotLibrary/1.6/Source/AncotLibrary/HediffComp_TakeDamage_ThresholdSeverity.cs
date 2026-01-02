using Verse;

namespace AncotLibrary;

public class HediffComp_TakeDamage_ThresholdSeverity : HediffComp
{
	private HediffCompProperties_TakeDamage_ThresholdSeverity Props => (HediffCompProperties_TakeDamage_ThresholdSeverity)props;

	public override void CompPostTick(ref float severityAdjustment)
	{
		if (parent.Severity >= Props.thresholdSeverity && !base.Pawn.Dead)
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
			parent.Severity -= Props.decreaseSeverityTriggered;
		}
	}
}
