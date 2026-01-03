using Verse;

namespace TOT_DLL_test;

internal class CompProperties_SecondaryVerb : CompProperties
{
	public VerbProperties verbProps = new VerbProperties();

	public Verb SecondaryVerb;

	public string mainCommandIcon = "";

	public string mainWeaponLabel = "";

	public string secondaryCommandIcon = "";

	public string secondaryWeaponLabel = "";

	public string description = "";

	public CompProperties_SecondaryVerb()
	{
		compClass = typeof(CompSecondaryVerb);
	}
}
