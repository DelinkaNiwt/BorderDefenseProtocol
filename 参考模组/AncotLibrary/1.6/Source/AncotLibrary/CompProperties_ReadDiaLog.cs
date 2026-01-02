using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompProperties_ReadDiaLog : CompProperties_Usable
{
	public TaggedString dialogOptionText;

	public TaggedString dialogContent;

	public TaggedString letterLabel;

	public TaggedString letterText;

	public LetterDef letterDef = LetterDefOf.NeutralEvent;

	public bool oneUse = false;

	public CompProperties_ReadDiaLog()
	{
		compClass = typeof(CompReadDiaLog);
	}
}
