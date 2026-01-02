using Verse;

namespace TOT_DLL_test;

public class HediffCompProperties_BoostedEffect : HediffCompProperties
{
	public float R = 255f;

	public float G = 255f;

	public float B = 255f;

	public float yOffset = 0f;

	public HediffCompProperties_BoostedEffect()
	{
		compClass = typeof(HediffComp_BoostedEffect);
	}
}
