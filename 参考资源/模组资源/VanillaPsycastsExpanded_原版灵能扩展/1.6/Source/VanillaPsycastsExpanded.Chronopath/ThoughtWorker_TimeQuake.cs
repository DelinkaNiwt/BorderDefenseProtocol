using RimWorld;
using Verse;

namespace VanillaPsycastsExpanded.Chronopath;

public class ThoughtWorker_TimeQuake : ThoughtWorker
{
	protected override ThoughtState CurrentStateInternal(Pawn p)
	{
		return p.Map.gameConditionManager.ConditionIsActive(VPE_DefOf.VPE_TimeQuake);
	}
}
