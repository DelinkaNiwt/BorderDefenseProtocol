using Verse;

namespace AncotLibrary;

public class CompProperties_CommandPivot : CompProperties
{
	public int gizmoOrder = -99;

	public bool onlyShowGizmoDrafted = false;

	public string gizmoLabel1;

	public string gizmoLabel2;

	public string gizmoDesc1;

	public string gizmoDesc2;

	public string gizmoIconPath1 = "AncotLibrary/Gizmos/Switch_I";

	public string gizmoIconPath2 = "AncotLibrary/Gizmos/Switch_II";

	public string iconPath = "AncotLibrary/Gizmos/SwitchA";

	public CompProperties_CommandPivot()
	{
		compClass = typeof(CompCommandPivot);
	}
}
