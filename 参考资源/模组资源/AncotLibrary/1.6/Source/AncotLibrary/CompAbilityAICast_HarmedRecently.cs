using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompAbilityAICast_HarmedRecently : CompAbilityEffect
{
	private new CompProperties_AICast_HarmedRecently Props => (CompProperties_AICast_HarmedRecently)props;

	public Pawn Caster => parent.pawn;

	public override bool AICanTargetNow(LocalTargetInfo target)
	{
		if (Caster.mindState.lastHarmTick > 0 && Find.TickManager.TicksGame < Caster.mindState.lastHarmTick + Props.thresholdTicks)
		{
			return true;
		}
		return false;
	}
}
