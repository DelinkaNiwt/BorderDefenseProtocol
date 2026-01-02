using Verse;

namespace AncotLibrary;

public class HediffCompProperties_PointDefense : HediffCompProperties
{
	public float range = 3f;

	public float severityCostPerDefense = 0f;

	public float availableSeverityThreshold = 0.01f;

	public float switchOffRestoreRate = 0.1f;

	public float switchOnConsumeRate = 0.05f;

	public float damageThreshold = 0f;

	public EffecterDef defenseEffecter;

	public string gizmoLabel = "";

	public string gizmoDesc = "";

	public string iconPath;

	public HediffCompProperties_PointDefense()
	{
		compClass = typeof(HediffComp_PointDefense);
	}
}
