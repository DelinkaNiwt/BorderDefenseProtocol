using Verse;

namespace NCL;

public class CompDamageReduction : ThingComp
{
	private CompProperties_DamageReduction Props => (CompProperties_DamageReduction)props;

	public float CurrentDamageFactor
	{
		get
		{
			if (!(parent is Pawn { Dead: false } pawn))
			{
				return 1f;
			}
			float healthPercent = pawn.health.summaryHealth.SummaryHealthPercent;
			if (healthPercent > Props.minHealthPercent)
			{
				return Props.minDamageFactor + (1f - Props.minDamageFactor) * ((healthPercent - Props.minHealthPercent) / (1f - Props.minHealthPercent));
			}
			return Props.minDamageFactor;
		}
	}

	public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
	{
		base.PostPreApplyDamage(ref dinfo, out absorbed);
		if (!absorbed && dinfo.Amount > 0f)
		{
			dinfo.SetAmount(dinfo.Amount * CurrentDamageFactor);
		}
	}
}
