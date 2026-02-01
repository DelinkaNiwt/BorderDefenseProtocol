using Verse;

namespace NCL;

public class CompProperties_AllDead : CompProperties
{
	public string toggleLabelKey;

	public string toggleDescKey;

	public string toggleIconPath;

	public string unableKey;

	public ThingDef mechanoidToKill;

	public CompProperties_AllDead()
	{
		compClass = typeof(CompAllDead);
	}
}
