using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompProperties_ReadLetter : CompProperties_Usable
{
	public TaggedString letterLabel;

	public TaggedString letterText;

	public LetterDef letterDef = LetterDefOf.NeutralEvent;

	public bool oneUse = false;

	public CompProperties_ReadLetter()
	{
		compClass = typeof(CompReadLetter);
	}
}
