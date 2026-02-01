using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using Verse;

namespace NCL;

public class DelayedPawnSpawner : MapComponent
{
	private List<DelayedSpawnInfo> delayedSpawns = new List<DelayedSpawnInfo>();

	public DelayedPawnSpawner(Map map)
		: base(map)
	{
	}

	public void RegisterDelayedSpawn(DelayedSpawnInfo info)
	{
		delayedSpawns.Add(info);
	}

	public override void MapComponentTick()
	{
		base.MapComponentTick();
		for (int i = delayedSpawns.Count - 1; i >= 0; i--)
		{
			DelayedSpawnInfo info = delayedSpawns[i];
			if (Find.TickManager.TicksGame >= info.spawnTick)
			{
				for (int j = 0; j < info.count; j++)
				{
					SpawnDelayedPawn(info);
				}
				delayedSpawns.RemoveAt(i);
			}
		}
	}

	private void SpawnDelayedPawn(DelayedSpawnInfo info)
	{
		if (info.caster == null || info.caster.Map == null || info.pawnKinds == null || info.pawnKinds.Count == 0)
		{
			return;
		}
		PawnKindDef pawnKind = info.pawnKinds.RandomElement();
		PawnGenerationRequest request = new PawnGenerationRequest(pawnKind, info.caster.Faction ?? Faction.OfPlayer, PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: true, allowDead: false, allowDowned: false, canGeneratePawnRelations: false, mustBeCapableOfViolence: true);
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
		if (CellFinder.TryFindRandomCellNear(info.caster.Position, info.caster.Map, info.spawnRadius, (IntVec3 c) => c.Walkable(info.caster.Map) && c.DistanceTo(info.caster.Position) >= (float)info.minDistance && c.GetFirstBuilding(info.caster.Map) == null, out var spawnPos))
		{
			DropPodUtility.DropThingsNear(spawnPos, info.caster.Map, new List<Thing> { newPawn }, 110, canInstaDropDuringInit: false, faction: info.caster.Faction, leaveSlag: info.leaveSlag, canRoofPunch: info.canRoofPunch, forbid: false);
		}
	}
}
