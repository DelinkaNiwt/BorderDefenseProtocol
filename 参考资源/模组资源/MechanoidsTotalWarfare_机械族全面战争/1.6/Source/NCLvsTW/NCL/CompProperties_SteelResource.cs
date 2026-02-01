using Verse;

namespace NCL;

public class CompProperties_SteelResource : CompProperties
{
	public string fixedIngredientDefName = "Steel";

	[Unsaved(false)]
	public ThingDef fixedIngredient;

	public int maxIngredientCount = 5000;

	public int startingIngredientCount = 1000;

	public CompProperties_SteelResource()
	{
		compClass = typeof(CompSteelResource);
	}
}
