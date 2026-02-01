using Verse;

namespace NCL;

public class CompDamageLimit : ThingComp
{
	private CompProperties_DamageLimit Props => (CompProperties_DamageLimit)props;

	public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
	{
		base.PostPreApplyDamage(ref dinfo, out absorbed);
		absorbed = false;
		if ((Props.excludedDamageTypes == null || !Props.excludedDamageTypes.Contains(dinfo.Def)) && dinfo.Amount > Props.maxDamage)
		{
			dinfo.SetAmount(Props.maxDamage);
		}
	}
}
