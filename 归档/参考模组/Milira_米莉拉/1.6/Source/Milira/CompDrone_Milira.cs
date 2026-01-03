using AncotLibrary;

namespace Milira;

public class CompDrone_Milira : CompDrone
{
	public override bool Draftable => MiliraDefOf.Milira_DroneControl.IsFinished;
}
