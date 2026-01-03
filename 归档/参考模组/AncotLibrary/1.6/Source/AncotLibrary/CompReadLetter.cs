using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompReadLetter : CompUsable
{
	public override void PostExposeData()
	{
		base.PostExposeData();
	}

	public override void Initialize(CompProperties props)
	{
		base.Initialize(props);
	}

	public override void UsedBy(Pawn pawn)
	{
		base.UsedBy(pawn);
		CompProperties_ReadLetter compProperties_ReadLetter = (CompProperties_ReadLetter)props;
		Find.LetterStack.ReceiveLetter(compProperties_ReadLetter.letterLabel, compProperties_ReadLetter.letterText, compProperties_ReadLetter.letterDef);
		if (compProperties_ReadLetter.oneUse)
		{
			parent.Destroy();
		}
	}
}
