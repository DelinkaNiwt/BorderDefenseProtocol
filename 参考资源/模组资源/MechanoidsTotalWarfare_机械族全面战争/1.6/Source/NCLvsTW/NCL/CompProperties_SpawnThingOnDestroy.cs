using RimWorld;
using Verse;

namespace NCL;

public class CompProperties_SpawnThingOnDestroy : CompProperties
{
	public ThingDef thingDef = null;

	public bool enableThingSpawn = true;

	public PawnKindDef pawnKindDef = null;

	public FactionDef faction = null;

	public bool enablePawnSpawn = false;

	public CompProperties_SpawnThingOnDestroy()
	{
		compClass = typeof(CompSpawnThingOnDestroy);
	}
}
