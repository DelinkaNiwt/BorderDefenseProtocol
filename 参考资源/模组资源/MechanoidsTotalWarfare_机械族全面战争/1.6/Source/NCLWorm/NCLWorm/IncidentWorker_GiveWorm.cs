using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace NCLWorm;

public class IncidentWorker_GiveWorm : IncidentWorker
{
	protected override bool TryExecuteWorker(IncidentParms parms)
	{
		Map map = parms.target as Map;
		map.mapPawns.AllPawnsSpawned.Where((Pawn x) => x.def.defName == "NCL_MechWorm").RandomElement()?.DeSpawn(DestroyMode.Refund);
		PawnKindDef named = DefDatabase<PawnKindDef>.GetNamed("NCL_MechWorm");
		Pawn item = PawnGenerator.GeneratePawn(named, Faction.OfPlayer);
		List<Thing> things = new List<Thing> { item };
		IntVec3 dropCenter = DropCellFinder.RandomDropSpot(map);
		DropPodUtility.DropThingsNear(dropCenter, map, things);
		return true;
	}
}
