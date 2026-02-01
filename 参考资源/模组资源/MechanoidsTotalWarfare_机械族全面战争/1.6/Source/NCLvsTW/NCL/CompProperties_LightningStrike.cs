using Verse;

namespace NCL;

public class CompProperties_LightningStrike : CompProperties
{
	public float requiredPower = 100000f;

	public int empRadius = 20;

	public int cooldownTicks = 60000;

	public bool consumeFromBatteriesOnly = true;

	public CompProperties_LightningStrike()
	{
		compClass = typeof(CompLightningStrike);
	}
}
