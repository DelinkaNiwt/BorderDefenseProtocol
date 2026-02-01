using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using Verse;

namespace NCL;

public class CompAbilityEffect_SummonDropPawns : CompAbilityEffect
{
	public new CompProperties_AbilitySummonDropPawns Props => (CompProperties_AbilitySummonDropPawns)props;

	public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
	{
		base.Apply(target, dest);
		if (parent.pawn.Map.GetComponent<DelayedPawnSpawner>() == null)
		{
			parent.pawn.Map.components.Add(new DelayedPawnSpawner(parent.pawn.Map));
		}
		for (int i = 0; i < Props.pawnCount; i++)
		{
			SpawnPawnAnywhere(parent.pawn, Props.pawnKinds);
		}
		if (Props.secondaryPawnCount > 0 && Props.secondaryPawnKinds != null && Props.secondaryPawnKinds.Count > 0)
		{
			parent.pawn.Map.GetComponent<DelayedPawnSpawner>().RegisterDelayedSpawn(new DelayedSpawnInfo(parent.pawn, Props.secondaryPawnKinds, Props.secondaryPawnCount, Props.spawnRadius, Props.minSpawnDistance, Props.leaveSlag, Props.canRoofPunch, Find.TickManager.TicksGame + 300));
		}
	}

	private void SpawnPawnAnywhere(Pawn caster, List<PawnKindDef> possibleKinds)
	{
		if (caster == null || caster.Map == null || possibleKinds == null || possibleKinds.Count == 0)
		{
			Log.Error("CompAbilityEffect_SummonDropPawns: 无效的生成参数!");
			return;
		}
		PawnKindDef pawnKind = possibleKinds.RandomElement();
		PawnGenerationRequest request = new PawnGenerationRequest(pawnKind, caster.Faction ?? Faction.OfPlayer, PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: true, allowDead: false, allowDowned: false, canGeneratePawnRelations: false, mustBeCapableOfViolence: true);
		Pawn newPawn = PawnGenerator.GeneratePawn(request);
		if (newPawn.ageTracker != null)
		{
			newPawn.ageTracker.AgeBiologicalTicks = 0L;
			newPawn.ageTracker.AgeChronologicalTicks = 0L;
		}
		CompMechPowerCell mechPowerComp = newPawn.GetComp<CompMechPowerCell>();
		if (mechPowerComp != null)
		{
			FieldInfo powerTicksLeftField = typeof(CompMechPowerCell).GetField("powerTicksLeft", BindingFlags.Instance | BindingFlags.NonPublic);
			if (powerTicksLeftField != null)
			{
				powerTicksLeftField.SetValue(mechPowerComp, mechPowerComp.Props.totalPowerTicks);
			}
			mechPowerComp.depleted = false;
		}
		IntVec3 spawnPos = FindNearestValidDropPosition(caster);
		DropPodUtility.DropThingsNear(spawnPos, caster.Map, new List<Thing> { newPawn }, 110, canInstaDropDuringInit: false, faction: caster.Faction, leaveSlag: Props.leaveSlag, canRoofPunch: Props.canRoofPunch, forbid: false);
	}

	private IntVec3 FindNearestValidDropPosition(Pawn caster)
	{
		Map map = caster.Map;
		IntVec3 casterPos = caster.Position;
		IntVec3 bestPos = IntVec3.Invalid;
		float bestDistance = float.MaxValue;
		for (int i = 0; i < map.cellIndices.NumGridCells; i++)
		{
			IntVec3 cell = map.cellIndices.IndexToCell(i);
			if (cell.IsValid && cell.InBounds(map) && IsValidDropPosition(map, cell))
			{
				float distance = cell.DistanceTo(casterPos);
				if (distance < bestDistance)
				{
					bestPos = cell;
					bestDistance = distance;
				}
			}
		}
		if (bestPos.IsValid)
		{
			return bestPos;
		}
		Log.Warning("未找到有效的无屋顶区域，降落在使用者附近");
		if (CellFinder.TryFindRandomCellNear(casterPos, map, Props.minSpawnDistance * 2, (IntVec3 c) => c.Standable(map) && c.GetFirstBuilding(map) == null, out var fallbackPos))
		{
			return fallbackPos;
		}
		return casterPos;
	}

	private bool IsValidDropPosition(Map map, IntVec3 cell)
	{
		return cell.Standable(map) && !cell.Roofed(map) && cell.GetFirstBuilding(map) == null && cell.DistanceTo(parent.pawn.Position) >= (float)Props.minSpawnDistance;
	}
}
