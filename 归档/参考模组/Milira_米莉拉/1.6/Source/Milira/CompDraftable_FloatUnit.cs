using AncotLibrary;

namespace Milira;

public class CompDraftable_FloatUnit : CompDraftable
{
	public override bool Draftable => MiliraDefOf.Milira_DroneControl.IsFinished;
}
