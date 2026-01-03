using Verse;

namespace AncotLibrary;

public class CompProperties_SpawnerCustom_PlantGrow : CompProperties_SpawnerCustom
{
	public float growDays = 6f;

	public FloatRange growTemperature = new FloatRange(0f, 58f);

	public float growlight = 0.51f;

	public CompProperties_SpawnerCustom_PlantGrow()
	{
		compClass = typeof(CompSpawnerCustom_PlantGrow);
	}
}
