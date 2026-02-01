using Verse;

namespace NCL;

public class CompProperties_FearFire : CompProperties
{
	public float detectionRadius = 5f;

	public int checkInterval = 30;

	public float fleeDistance = 10f;

	public float minSafeDistance = 3f;

	public bool affectAnimals = true;

	public bool affectHumans = true;

	public bool affectMechanoids = true;

	public bool showVisualEffects = true;

	public bool showPanicText = true;

	public float yellChance = 0.7f;

	public bool showDebugRadius = false;

	public CompProperties_FearFire()
	{
		compClass = typeof(CompFearFire);
	}
}
