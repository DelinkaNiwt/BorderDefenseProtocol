using Verse;

namespace AncotLibrary;

public class CompProperties_RepairMechArea : CompProperties
{
	public bool applyAllyOnly = true;

	public int hitpointPerRepair = 5;

	public int repairTicks = 120;

	public float radius = 10f;

	public CompProperties_RepairMechArea()
	{
		compClass = typeof(CompRepairMechArea);
	}
}
