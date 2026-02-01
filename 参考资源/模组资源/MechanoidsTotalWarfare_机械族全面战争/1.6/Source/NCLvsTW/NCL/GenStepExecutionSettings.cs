using Verse;

namespace NCL;

public class GenStepExecutionSettings
{
	public static GenStepDef SelectedGenStep;

	public static bool ShowExecutionButton = true;

	public static void ExposeData()
	{
		Scribe_Defs.Look(ref SelectedGenStep, "selectedGenStep");
		Scribe_Values.Look(ref ShowExecutionButton, "showExecutionButton", defaultValue: true);
	}
}
