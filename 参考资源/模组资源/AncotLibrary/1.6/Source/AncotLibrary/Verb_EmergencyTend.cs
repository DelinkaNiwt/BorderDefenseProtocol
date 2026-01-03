using RimWorld;
using Verse;

namespace AncotLibrary;

public class Verb_EmergencyTend : Verb_Job
{
	protected override bool TryCastShot()
	{
		Pawn pawn = currentTarget.Pawn;
		if (pawn == null)
		{
			pawn = CasterPawn;
		}
		if (pawn != null && pawn.health.hediffSet.BleedRateTotal > 0.001f)
		{
			return TryGiveJob(base.EquipmentSource.TryGetComp<CompApparelVerbOwner_Charged>(), currentTarget);
		}
		Messages.Message("Ancot.targetNeedNoTend".Translate(), pawn, MessageTypeDefOf.NeutralEvent);
		return false;
	}
}
