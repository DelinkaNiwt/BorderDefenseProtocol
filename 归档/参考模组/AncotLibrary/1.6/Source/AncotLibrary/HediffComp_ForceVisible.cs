using System.Collections.Generic;
using Verse;

namespace AncotLibrary;

public class HediffComp_ForceVisible : HediffComp
{
	private HediffCompProperties_ForceVisible Props => (HediffCompProperties_ForceVisible)props;

	public override void CompPostTickInterval(ref float severityAdjustment, int delta)
	{
		if (!base.Pawn.Spawned)
		{
			return;
		}
		List<Hediff> hediffs = base.Pawn.health.hediffSet.hediffs;
		for (int i = 0; i < hediffs.Count; i++)
		{
			HediffComp_Invisibility hediffComp_Invisibility = hediffs[i].TryGetComp<HediffComp_Invisibility>();
			if (hediffComp_Invisibility != null && !hediffComp_Invisibility.PsychologicallyVisible)
			{
				hediffComp_Invisibility.BecomeVisible();
			}
		}
	}
}
