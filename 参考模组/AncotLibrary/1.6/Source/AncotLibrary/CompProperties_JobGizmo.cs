using Verse;

namespace AncotLibrary;

public class CompProperties_JobGizmo : CompProperties
{
	public bool showGizmoUndrafted = false;

	public string gizmoLabel = "Take Job";

	public string gizmoDesc = "Try Take Job Once";

	public string gizmoIconPath = "AncotLibrary/Gizmos/SwitchA";

	public JobDef jobDef;

	public int gizmoOrder = 0;

	public CompProperties_JobGizmo()
	{
		compClass = typeof(CompJobGizmo);
	}
}
