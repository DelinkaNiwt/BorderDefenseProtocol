using Verse;

namespace AncotLibrary;

public class CompProperties_PointDefense : CompProperties
{
	public float range = 3f;

	public float damageThreshold = 0f;

	public EffecterDef defenseEffecter;

	public int consumeChargeAmount = 0;

	public string gizmoLabel = "";

	public string gizmoDesc = "";

	public string iconPath = "AncotLibrary/Gizmos/SwitchA";

	public bool alwaysShowGizmo = false;

	public bool ai_AlwaysSwitchOn = false;

	public CompProperties_PointDefense()
	{
		compClass = typeof(CompPointDefense);
	}
}
