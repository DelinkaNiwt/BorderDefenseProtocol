using Verse;

namespace AncotLibrary;

public class HediffCompProperties_ThingCarrierCountOverride : HediffCompProperties
{
	public int maxIngredientCountOffset = 100;

	public HediffCompProperties_ThingCarrierCountOverride()
	{
		compClass = typeof(HediffComp_ThingCarrierCountOverride);
	}
}
