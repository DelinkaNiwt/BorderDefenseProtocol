using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace Milira;

public class QuestNode_GetPawn_FallenAngel : QuestNode
{
	[NoTranslate]
	public SlateRef<string> storeAs;

	protected override bool TestRunInt(Slate slate)
	{
		Pawn pawn = Current.Game.GetComponent<MiliraGameComponent_OverallControl>().pawn;
		if (pawn == null || pawn.HostFaction != Faction.OfPlayer)
		{
			return false;
		}
		return !storeAs.GetValue(slate).NullOrEmpty();
	}

	protected override void RunInt()
	{
		Slate slate = QuestGen.slate;
		Pawn pawn = Current.Game.GetComponent<MiliraGameComponent_OverallControl>().pawn;
		slate.Set(storeAs.GetValue(slate), pawn);
	}
}
