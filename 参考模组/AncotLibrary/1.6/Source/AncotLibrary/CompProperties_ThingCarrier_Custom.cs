using Verse;

namespace AncotLibrary;

public class CompProperties_ThingCarrier_Custom : CompProperties
{
	public ThingDef fixedIngredient;

	public int maxIngredientCount;

	public int startingIngredientCount = 0;

	public bool startingLoadForPlayer = false;

	public int initialMaxToFillSetting = 0;

	public string savePrefix = "";

	public CompProperties_ThingCarrier_Custom()
	{
		compClass = typeof(CompThingCarrier_Custom);
	}
}
