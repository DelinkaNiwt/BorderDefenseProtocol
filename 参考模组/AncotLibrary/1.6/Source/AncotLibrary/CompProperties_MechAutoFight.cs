using Verse;

namespace AncotLibrary;

public class CompProperties_MechAutoFight : CompProperties
{
	public HediffDef hediffDef;

	public ResearchProjectDef requireResearch;

	public int gizmoOrder = -99;

	public string gizmoLabel;

	public string gizmoDesc;

	public string gizmoIconPath = "AncotLibrary/Gizmos/Auto";

	public CompProperties_MechAutoFight()
	{
		compClass = typeof(CompMechAutoFight);
	}
}
