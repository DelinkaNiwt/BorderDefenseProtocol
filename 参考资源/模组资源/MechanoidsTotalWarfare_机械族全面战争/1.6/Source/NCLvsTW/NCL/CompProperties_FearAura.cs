using Verse;

namespace NCL;

public class CompProperties_FearAura : CompProperties
{
	public float radius = 6f;

	public int checkInterval = 60;

	public float fleeChance = 1f;

	public bool affectAnimals = true;

	public bool affectHumans = true;

	public bool affectMechanoids = true;

	public bool affectAllies = false;

	public CompProperties_FearAura()
	{
		compClass = typeof(CompFearAura);
	}
}
