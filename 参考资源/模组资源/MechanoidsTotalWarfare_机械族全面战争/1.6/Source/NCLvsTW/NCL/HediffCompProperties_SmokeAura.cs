using Verse;

namespace NCL;

public class HediffCompProperties_SmokeAura : HediffCompProperties
{
	public float minDistance = 5f;

	public float maxDistance = 6f;

	public float smokeSize = 5f;

	public SoundDef soundDef;

	public int smokeInterval = 120;

	public int smokesPerBurst = 3;

	public HediffCompProperties_SmokeAura()
	{
		compClass = typeof(HediffComp_SmokeAura);
	}
}
