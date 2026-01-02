using UnityEngine;
using Verse;

namespace AncotLibrary;

public class HediffCompApplyDamage_SeverityChange : HediffComp
{
	private HediffCompProperties_ApplyDamage_SeverityChange Props => (HediffCompProperties_ApplyDamage_SeverityChange)props;

	public override void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
	{
		if (!Props.explosiveOnly || dinfo.Def.isExplosive)
		{
			parent.Severity = Mathf.Clamp(parent.Severity + Props.severityChange, Props.minSeverity, Props.maxSeverity);
			if (Props.removeHediff)
			{
				parent.Severity -= parent.Severity;
			}
		}
	}
}
