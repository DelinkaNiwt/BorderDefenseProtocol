using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace AncotLibrary;

public class GenStep_GenPawnAroundMapCenter_Assist : GenStep
{
	public int fixedPoint = 0;

	public FactionDef factionDef;

	public bool canTimeoutOrFlee = false;

	public bool spawnInRect = false;

	public bool allowFlee = true;

	public int contractCell = 10;

	public float? contractCellPctOfInterestRect;

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
				IntVec3 result;
				if (spawnInRect)
				{
					if (contractCellPctOfInterestRect.HasValue)
					{
						contractCell = (int)((float)rectToDefend.Size.x * contractCellPctOfInterestRect).Value;
					}
					if (!CellFinder.TryFindRandomCellInsideWith(rectToDefend.ContractedBy(contractCell, contractCell), (IntVec3 c) => c.Standable(map) && c.InBounds(map), out result))
					{
						Find.WorldPawns.PassToWorld(item);
						break;
					}
				}
				else if (!SiteGenStepUtility.TryFindSpawnCellAroundOrNear(rectToDefend, singleCellToSpawnNear, map, out result))
				{
					Find.WorldPawns.PassToWorld(item);
					break;
				}
				GenSpawn.Spawn(item, result, map);
				list.Add(item);
			}
		}
		if (pawnkindsRequired != null)
		{
			foreach (Pawn item2 in AncotPawnGenUtility.GenerateRequiredPawns(faction, pawnkindsRequired))
			{
				IntVec3 result2;
				if (spawnInRect)
				{
					if (contractCellPctOfInterestRect.HasValue)
					{
						contractCell = (int)((float)rectToDefend.Size.x * contractCellPctOfInterestRect).Value;
					}
					if (!CellFinder.TryFindRandomCellInsideWith(rectToDefend.ContractedBy(contractCell, contractCell), (IntVec3 c) => c.Standable(map) && c.InBounds(map), out result2))
					{
						Find.WorldPawns.PassToWorld(item2);
						break;
					}
				}
				else if (!SiteGenStepUtility.TryFindSpawnCellAroundOrNear(rectToDefend, singleCellToSpawnNear, map, out result2))
				{
					Find.WorldPawns.PassToWorld(item2);
					break;
				}
				GenSpawn.Spawn(item2, result2, map);
				list.Add(item2);
			}
		}
		if (list.Any())
		{
			if (!allowFlee)
			{
				LordMaker.MakeNewLord(faction, new LordJob_AssistColony_NoFlee(Faction.OfPlayer, map.Center), map, list);
			}
			else
			{
				LordMaker.MakeNewLord(faction, new LordJob_AssistColony(Faction.OfPlayer, map.Center), map, list);
			}
		}
	}
}
