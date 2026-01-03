using Verse;

namespace AncotLibrary;

public class CompProperties_Drone : CompProperties
{
	public bool showGizmoOnNonPlayerControlled;

	[MustTranslate]
	public string labelOverride;

	[MustTranslate]
	public string tooltipOverride;

	public DroneWorkModeDef initialWorkMode;

	public CompProperties_Drone()
	{
		compClass = typeof(CompDrone);
	}
}
