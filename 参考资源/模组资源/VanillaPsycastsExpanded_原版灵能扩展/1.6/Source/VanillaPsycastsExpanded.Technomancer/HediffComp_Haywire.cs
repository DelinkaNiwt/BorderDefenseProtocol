using Verse;
using Verse.AI;

namespace VanillaPsycastsExpanded.Technomancer;

public class HediffComp_Haywire : HediffComp
{
	public override void CompPostPostAdd(DamageInfo? dinfo)
	{
		base.CompPostPostAdd(dinfo);
		HaywireManager.HaywireThings.Add(base.Pawn);
		base.Pawn.stances?.CancelBusyStanceHard();
		base.Pawn.jobs?.EndCurrentJob(JobCondition.InterruptForced);
	}

	public override void CompPostPostRemoved()
	{
		base.CompPostPostRemoved();
		HaywireManager.HaywireThings.Remove(base.Pawn);
		base.Pawn.stances?.CancelBusyStanceHard();
		base.Pawn.jobs?.EndCurrentJob(JobCondition.InterruptForced);
	}
}
