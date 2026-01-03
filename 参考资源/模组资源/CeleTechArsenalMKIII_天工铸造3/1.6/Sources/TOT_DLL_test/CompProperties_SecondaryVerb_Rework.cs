using Verse;

namespace TOT_DLL_test;

internal class CompProperties_SecondaryVerb_Rework : CompProperties
{
	public string mainCommandIcon = "";

	public string mainWeaponLabel = "";

	public string secondaryCommandIcon = "";

	public string secondaryWeaponLabel = "";

	public string description = "";

	public VerbProperties verbProps = new VerbProperties();

	public CompProperties_SecondaryVerb_Rework()
	{
		compClass = typeof(CompSecondaryVerb_Rework);
	}
}
