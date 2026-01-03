using RimWorld;
using UnityEngine;
using Verse;

namespace AlienRace;

public class ThoughtWorker_Precept_NoRecentAlienMeat : ThoughtWorker_Precept
{
	private const int DAYS_WITHOUT_ALIEN_MEAT = 8;

	protected override ThoughtState ShouldHaveThought(Pawn p)
	{
		int num = Mathf.Max(0, p.GetComp<AlienPartGenerator.AlienComp>()?.lastAlienMeatIngestedTick ?? GenTicks.TicksGame);
		return Find.TickManager.TicksGame - num > 480000;
	}

	public override string PostProcessDescription(Pawn p, string description)
	{
		return base.PostProcessDescription(p, description).Formatted(8.Named("HUMANMEATREQUIREDINTERVAL"));
	}
}
