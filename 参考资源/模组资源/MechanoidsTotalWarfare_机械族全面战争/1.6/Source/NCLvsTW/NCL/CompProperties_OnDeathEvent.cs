using RimWorld;
using Verse;

namespace NCL;

public class CompProperties_OnDeathEvent : CompProperties
{
	public IncidentDef incidentToTrigger;

	public bool spawnThingOnDeath;

	public ThingDef thingToSpawn;

	public int spawnCount = 1;

	public CompProperties_OnDeathEvent()
	{
		compClass = typeof(CompOnDeathEvent);
	}
}
