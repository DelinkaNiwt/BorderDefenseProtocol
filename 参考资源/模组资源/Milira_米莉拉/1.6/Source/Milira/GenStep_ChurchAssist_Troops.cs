using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace Milira;

public class GenStep_ChurchAssist_Troops : GenStep
{
	public int fixedPoint = 0;

	public override int SeedPart => 341124587;

	public override void Generate(Map map, GenStepParams parms)
	{
		Faction faction = Find.FactionManager.FirstFactionOfDef(MiliraDefOf.Milira_AngelismChurch);
		List<Pawn> list = new List<Pawn>();
		IntVec3 intVec = FindNearEdgeCell(map, null);
		foreach (Pawn item in GeneratePawns(parms, map))
		{
			GenSpawn.Spawn(item, CellFinder.RandomSpawnCellForPawnNear(intVec, map), map);
			list.Add(item);
		}
		if (list.Any())
		{
			LordMaker.MakeNewLord(faction, new LordJob_AssistColony(faction, intVec), map, list);
		}
	}

	private IEnumerable<Pawn> GeneratePawns(GenStepParams parms, Map map)
	{
		Faction faction = Find.FactionManager.FirstFactionOfDef(MiliraDefOf.Milira_AngelismChurch);
		float points = ((parms.sitePart != null) ? parms.sitePart.parms.threatPoints : 500f);
		PawnGroupMakerParms pawnGroupMakerParms = new PawnGroupMakerParms();
		pawnGroupMakerParms.groupKind = PawnGroupKindDefOf.Combat;
		pawnGroupMakerParms.tile = map.Tile;
		pawnGroupMakerParms.faction = faction;
		pawnGroupMakerParms.points = points;
		if (fixedPoint != 0)
		{
			pawnGroupMakerParms.points = fixedPoint;
		}
		if (parms.sitePart != null)
		{
			pawnGroupMakerParms.seed = SleepingMechanoidsSitePartUtility.GetPawnGroupMakerSeed(parms.sitePart.parms);
		}
		return PawnGroupMakerUtility.GeneratePawns(pawnGroupMakerParms);
	}

	public IntVec3 FindNearEdgeCell(Map map, Predicate<IntVec3> extraCellValidator)
	{
		Predicate<IntVec3> baseValidator = (IntVec3 x) => x.Standable(map) && !x.Fogged(map) && map.reachability.CanReachUnfogged(x, TraverseParms.For(TraverseMode.PassDoors));
		Faction parentFaction = map.ParentFaction;
		if (CellFinder.TryFindRandomEdgeCellWith((IntVec3 x) => baseValidator(x) && (extraCellValidator == null || extraCellValidator(x)), map, CellFinder.EdgeRoadChance_Neutral, out var result))
		{
			return CellFinder.RandomClosewalkCellNear(result, map, 5);
		}
		if (extraCellValidator != null && CellFinder.TryFindRandomEdgeCellWith((IntVec3 x) => baseValidator(x) && extraCellValidator(x), map, CellFinder.EdgeRoadChance_Neutral, out result))
		{
			return CellFinder.RandomClosewalkCellNear(result, map, 5);
		}
		if (CellFinder.TryFindRandomEdgeCellWith(baseValidator, map, CellFinder.EdgeRoadChance_Neutral, out result))
		{
			return CellFinder.RandomClosewalkCellNear(result, map, 5);
		}
		Log.Warning("Could not find any valid edge cell.");
		return CellFinder.RandomClosewalkCellNear(result, map, 5);
	}
}
