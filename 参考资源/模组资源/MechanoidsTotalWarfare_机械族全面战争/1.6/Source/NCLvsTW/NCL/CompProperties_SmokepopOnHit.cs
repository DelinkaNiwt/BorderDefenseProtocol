using Verse;

namespace NCL;

public class CompProperties_SmokepopOnHit : CompProperties
{
	public float cooldownSeconds = 10f;

	public float smokeRadius = 3f;

	public int smokeDurationTicks = 600;

	public SoundDef soundOnActivate;

	public CompProperties_SmokepopOnHit()
	{
		compClass = typeof(CompSmokepopOnHit);
	}
}
