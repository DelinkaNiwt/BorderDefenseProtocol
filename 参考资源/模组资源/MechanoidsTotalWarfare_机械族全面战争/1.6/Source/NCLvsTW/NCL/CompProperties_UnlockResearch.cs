using Verse;

namespace NCL;

public class CompProperties_UnlockResearch : CompProperties
{
	public ResearchProjectDef researchToUnlock;

	public float activateRange = 3f;

	public bool showActivationEffect = true;

	public int checkIntervalTicks = 60;

	public bool requireLineOfSight = true;

	public bool onlyAwakeColonists = true;

	public LetterDef letterDef;

	public string letterLabel;

	public string letterText;

	public bool sendLetter = true;

	public CompProperties_UnlockResearch()
	{
		compClass = typeof(CompUnlockResearch);
	}
}
