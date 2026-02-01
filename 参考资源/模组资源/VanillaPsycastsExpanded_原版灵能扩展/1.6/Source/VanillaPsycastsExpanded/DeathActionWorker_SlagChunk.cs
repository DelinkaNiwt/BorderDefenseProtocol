using RimWorld;
using Verse;
using Verse.AI.Group;

namespace VanillaPsycastsExpanded;

public class DeathActionWorker_SlagChunk : DeathActionWorker
{
	public override void PawnDied(Corpse corpse, Lord prevLord)
	{
		if (corpse.Map != null)
		{
			GenSpawn.Spawn(ThingMaker.MakeThing(ThingDefOf.ChunkSlagSteel), corpse.Position, corpse.Map);
			corpse.Destroy();
		}
	}
}
