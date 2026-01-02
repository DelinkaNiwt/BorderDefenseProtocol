using RimWorld;

namespace Milira;

public class CompProperties_AbilityCheckCarrier : CompProperties_AbilityEffect
{
	public int minIngredientCount = 0;

	public int ingredientCost = 0;

	public CompProperties_AbilityCheckCarrier()
	{
		compClass = typeof(CompAbilityCheckCarrier);
	}
}
