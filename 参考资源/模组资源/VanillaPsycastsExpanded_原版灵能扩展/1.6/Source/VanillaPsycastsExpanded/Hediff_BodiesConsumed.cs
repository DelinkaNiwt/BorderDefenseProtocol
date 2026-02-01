using Verse;

namespace VanillaPsycastsExpanded;

public class Hediff_BodiesConsumed : HediffWithComps
{
	public int consumedBodies;

	public override string Label => base.Label + ": " + consumedBodies;

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref consumedBodies, "consumedBodies", 0);
	}
}
