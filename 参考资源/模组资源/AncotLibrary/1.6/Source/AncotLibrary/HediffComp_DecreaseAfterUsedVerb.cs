using Verse;

namespace AncotLibrary;

public class HediffComp_DecreaseAfterUsedVerb : HediffComp
{
	private HediffCompProperties_DecreaseAfterUsedVerb Props => (HediffCompProperties_DecreaseAfterUsedVerb)props;

	public override void Notify_PawnUsedVerb(Verb verb, LocalTargetInfo target)
	{
		if (!Props.verbShootOnly || verb.GetProjectile() != null)
		{
			if (parent.Severity - Props.severityPerUse > Props.minSeverity)
			{
				parent.Severity -= Props.severityPerUse;
			}
			else
			{
				parent.Severity = Props.minSeverity;
			}
		}
	}
}
