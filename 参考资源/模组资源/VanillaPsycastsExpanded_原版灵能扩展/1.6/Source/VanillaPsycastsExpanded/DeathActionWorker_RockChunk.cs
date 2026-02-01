using Verse;
using Verse.AI.Group;

namespace VanillaPsycastsExpanded;

public class DeathActionWorker_RockChunk : DeathActionWorker
{
	public override void PawnDied(Corpse corpse, Lord prevLord)
	{
		if (corpse.Map != null)
		{
			ThingDef thingDef = corpse.InnerPawn?.TryGetComp<CompSetStoneColour>()?.KilledLeave;
			if (thingDef != null)
			{
				GenSpawn.Spawn(ThingMaker.MakeThing(thingDef), corpse.Position, corpse.Map);
				corpse.Destroy();
			}
		}
	}
}
