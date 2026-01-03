using Verse;

namespace AncotLibrary;

public class CompProperties_SustainableAttractive : CompProperties
{
	public float range;

	public float damageRange;

	public float distance;

	public int intervalTick = 60;

	public CompProperties_SustainableAttractive()
	{
		compClass = typeof(CompSustainableAttractive);
	}
}
