using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace AncotLibrary;

public class GenStep_GenPawnAroundMapCenter_Attack : GenStep
{
	public int fixedPoint = 0;

	public FactionDef factionDef;

	public bool canTimeoutOrFlee = false;

	public List<PawnkindWithAmount> pawnkindsRequired;

	public List<PawnkindWithCommonality> pawnkindsWithCommonality;

	public override int SeedPart => 341125487 + Rand.Range(0, 99999);

	public override void Generate(Map map, GenStepParams parms)
	{
		if (!SiteGenStepUtility.TryFindRootToSpawnAroundRectOfInterest(out var rectToDefend, out var singleCellToSpawnNear, map))
		{
			return;
		}
		Faction faction = Find.FactionManager.FirstFactionOfDef(factionDef);
		List<Pawn> list = new List<Pawn>();
		if (pawnkindsWithCommonality != null)
		{
			foreach (Pawn item in AncotPawnGenUtility.GeneratePawnsWithCommonality(parms, faction, map, fixedPoint, pawnkindsWithCommonality))
			{
				if (!SiteGenStepUtility.TryFindSpawnCellAroundOrNear(rectToDefend, singleCellToSpawnNear, map, out var spawnCell))
				{
					Find.WorldPawns.PassToWorld(item);
					break;
				}
				GenSpawn.Spawn(item, spawnCell, map);
				list.Add(item);
			}
		}
		if (pawnkindsRequired != null)
		{
			foreach (Pawn item2 in AncotPawnGenUtility.GenerateRequiredPawns(faction, pawnkindsRequired))
			{
				if (!SiteGenStepUtility.TryFindSpawnCellAroundOrNear(rectToDefend, singleCellToSpawnNear, map, out var spawnCell2))
				{
					Find.WorldPawns.PassToWorld(item2);
					break;
				}
				GenSpawn.Spawn(item2, spawnCell2, map);
				list.Add(item2);
			}
		}
		if (list.Any())
		{
			LordMaker.MakeNewLord(faction, new LordJob_AssaultColony(faction, canKidnap: true, canTimeoutOrFlee), map, list);
		}
	}
}
