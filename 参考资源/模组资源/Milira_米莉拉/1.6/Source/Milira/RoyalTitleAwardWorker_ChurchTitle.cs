using AncotLibrary;
using RimWorld;
using Verse;

namespace Milira;

public class RoyalTitleAwardWorker_ChurchTitle : RoyalTitleAwardWorker_SendLetter
{
	public override void SendLetter(Pawn pawn, Faction faction, RoyalTitleDef currentTitle, RoyalTitleDef newTitle)
	{
		Find.LetterStack.ReceiveLetter("Milira.ChurchTitleLetter".Translate(pawn, newTitle.label), "Milira.ChurchTitleLetterDesc".Translate(pawn, faction, newTitle.label), LetterDefOf.PositiveEvent);
	}
}
