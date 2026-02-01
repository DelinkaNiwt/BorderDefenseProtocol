using RimWorld;
using Verse;

namespace NCL;

public class CompOnDeathEvent : ThingComp
{
	public CompProperties_OnDeathEvent Props => (CompProperties_OnDeathEvent)props;

	public override void PostDestroy(DestroyMode mode, Map map)
	{
		if (mode == DestroyMode.KillFinalize)
		{
			if (Props.incidentToTrigger != null)
			{
				IncidentParms parms = new IncidentParms
				{
					target = map,
					spawnCenter = parent.Position
				};
				Props.incidentToTrigger.Worker.TryExecute(parms);
			}
			if (Props.spawnThingOnDeath && Props.thingToSpawn != null)
			{
				Thing thing = ThingMaker.MakeThing(Props.thingToSpawn);
				thing.stackCount = Props.spawnCount;
				GenSpawn.Spawn(thing, parent.Position, map);
			}
		}
		base.PostDestroy(mode, map);
	}
}
