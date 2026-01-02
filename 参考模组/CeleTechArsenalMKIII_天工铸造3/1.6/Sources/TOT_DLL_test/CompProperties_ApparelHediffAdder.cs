using Verse;

namespace TOT_DLL_test;

public class CompProperties_ApparelHediffAdder : CompProperties
{
	public string Label = "Default Label";

	public string UIPath;

	public string HediffName = "PsychicInvisibility";

	public int HediffTickToDisappear = 1200;

	public CompProperties_ApparelHediffAdder()
	{
		compClass = typeof(Comp_ApparelHediffAdder);
	}
}
