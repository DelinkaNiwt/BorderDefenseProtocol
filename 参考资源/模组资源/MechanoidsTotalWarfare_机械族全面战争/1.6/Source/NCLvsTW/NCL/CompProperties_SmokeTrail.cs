using Verse;

namespace NCL;

public class CompProperties_SmokeTrail : CompProperties
{
	public int intervalTicks = 10;

	public float smokeSize = 1f;

	public bool playSound = false;

	public SoundDef soundDef;

	public CompProperties_SmokeTrail()
	{
		compClass = typeof(Comp_SmokeTrail);
	}
}
