using RimWorld;
using Verse;

namespace AncotLibrary;

public class RoyalTitleAwardWorker_SendLetter : RoyalTitleAwardWorker
{
	public override void DoAward(Pawn pawn, Faction faction, RoyalTitleDef currentTitle, RoyalTitleDef newTitle)
	{
		SendLetter(pawn, faction, currentTitle, newTitle);
	}

	public virtual void SendLetter(Pawn pawn, Faction faction, RoyalTitleDef currentTitle, RoyalTitleDef newTitle)
	{
	}
}
