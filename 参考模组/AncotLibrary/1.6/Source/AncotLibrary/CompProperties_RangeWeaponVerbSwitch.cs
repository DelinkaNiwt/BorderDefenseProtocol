using Verse;

namespace AncotLibrary;

public class CompProperties_RangeWeaponVerbSwitch : CompProperties
{
	public int gizmoOrder = -99;

	public bool onlyShowGizmoDrafted = false;

	public string gizmoLabel1;

	public string gizmoLabel2;

	public string gizmoDesc1;

	public string gizmoDesc2;

	public string gizmoIconPath1 = "AncotLibrary/Gizmos/Switch_I";

	public string gizmoIconPath2 = "AncotLibrary/Gizmos/Switch_II";

	public float? aiInitialSwitchChance;

	public VerbProperties verbProps = new VerbProperties();

	public CompProperties_RangeWeaponVerbSwitch()
	{
		compClass = typeof(CompRangeWeaponVerbSwitch);
	}
}
