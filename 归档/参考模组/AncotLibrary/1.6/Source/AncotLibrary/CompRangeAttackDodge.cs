using RimWorld;
using Verse;

namespace AncotLibrary;

[StaticConstructorOnStartup]
public class CompRangeAttackDodge : ThingComp
{
	public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
	{
		absorbed = false;
		if (dinfo.Def.isRanged && !dinfo.Def.isExplosive && Rand.Chance(parent.GetStatValue(AncotDefOf.Ancot_RangeDodgeChance)))
		{
			AbsorbedDamage(dinfo);
			absorbed = true;
		}
	}

	private void AbsorbedDamage(DamageInfo dinfo)
	{
		MoteMaker.ThrowText(parent.DrawPos, parent.Map, "Ancot.TextMote_Dodge".Translate(), 1.9f);
	}
}
