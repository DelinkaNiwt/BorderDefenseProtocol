using System.Linq;
using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompGetHediff_ApplyDamage : ThingComp
{
	protected Pawn PawnOwner
	{
		get
		{
			if (!(parent is Apparel { Wearer: var wearer }))
			{
				if (parent is Pawn result)
				{
					return result;
				}
				return null;
			}
			return wearer;
		}
	}

	private CompProperties_GetHediff_ApplyDamage Props => (CompProperties_GetHediff_ApplyDamage)props;

	public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
	{
		base.PostPreApplyDamage(ref dinfo, out absorbed);
		Hediff firstHediffOfDef = PawnOwner.health.hediffSet.GetFirstHediffOfDef(Props.hediffDef);
		BodyPartRecord part = PawnOwner.health.hediffSet.GetNotMissingParts().FirstOrDefault((BodyPartRecord x) => x.def == Props.bodyPartDef);
		if (!PawnOwner.Dead && firstHediffOfDef == null)
		{
			PawnOwner.health.AddHediff(Props.hediffDef, part);
		}
		else
		{
			firstHediffOfDef.Severity += Props.severityPerHit;
		}
	}
}
