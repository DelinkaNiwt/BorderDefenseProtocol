using RimWorld;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public class JobGiver_AIFollowPivot : JobGiver_AIFollowPawn
{
	public float followRadius = 5f;

	protected override int FollowJobExpireInterval => 200;

	protected override Pawn GetFollowee(Pawn pawn)
	{
		return pawn.TryGetComp<CompCommandTerminal>().pivot;
	}

	protected override float GetRadius(Pawn pawn)
	{
		return followRadius;
	}

	protected override Job TryGiveJob(Pawn pawn)
	{
		if (pawn.TryGetComp<CompCommandTerminal>().pivot == null)
		{
			return null;
		}
		return base.TryGiveJob(pawn);
	}
}
