using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace Milira;

public class QuestNode_GetPawn_FallenAngelInColony : QuestNode
{
	[NoTranslate]
	public SlateRef<string> storeAs;

	protected override bool TestRunInt(Slate slate)
	{
		Pawn pawnInColony = Current.Game.GetComponent<MiliraGameComponent_OverallControl>().pawnInColony;
		if (pawnInColony == null || pawnInColony.Faction != Faction.OfPlayer)
		{
			return false;
		}
		return !storeAs.GetValue(slate).NullOrEmpty();
	}

	protected override void RunInt()
	{
		Slate slate = QuestGen.slate;
		Pawn pawnInColony = Current.Game.GetComponent<MiliraGameComponent_OverallControl>().pawnInColony;
		slate.Set(storeAs.GetValue(slate), pawnInColony);
	}
}
