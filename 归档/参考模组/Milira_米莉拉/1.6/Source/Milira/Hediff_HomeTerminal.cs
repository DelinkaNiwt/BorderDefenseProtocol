using RimWorld;
using Verse;

namespace Milira;

public class Hediff_HomeTerminal : HediffWithComps
{
	public override void PostAdd(DamageInfo? dinfo)
	{
		base.PostAdd(dinfo);
		if (!ModLister.CheckBiotech("Mechlink"))
		{
			pawn.health.RemoveHediff(this);
		}
		else
		{
			PawnComponentsUtility.AddAndRemoveDynamicComponents(pawn);
		}
	}

	public override void PostRemoved()
	{
		base.PostRemoved();
		if (!MechanitorUtility.ShouldBeMechanitor(pawn))
		{
			pawn.mechanitor?.Notify_MechlinkRemoved();
		}
	}
}
