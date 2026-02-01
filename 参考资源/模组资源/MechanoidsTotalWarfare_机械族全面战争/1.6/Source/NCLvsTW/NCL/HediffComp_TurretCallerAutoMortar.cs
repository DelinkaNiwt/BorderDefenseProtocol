using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace NCL;

public class HediffComp_TurretCallerAutoMortar : HediffComp
{
	private bool activated = false;

	public HediffCompProperties_TurretCallerAutoMortar Props => (HediffCompProperties_TurretCallerAutoMortar)props;

	public bool CanApply => base.Pawn != null && base.Pawn.Spawned && !base.Pawn.Dead && !base.Pawn.Downed && base.Pawn.Faction != null && base.Pawn.Faction.def == FactionDefOf.Mechanoid && !IsDormant();

	private bool IsDormant()
	{
		CompCanBeDormant dormantComp = base.Pawn.GetComp<CompCanBeDormant>();
		if (dormantComp != null)
		{
			return !dormantComp.Awake;
		}
		return false;
	}

	public override void CompPostTick(ref float severityAdjustment)
	{
		base.CompPostTick(ref severityAdjustment);
		if (CanApply && !activated && Find.TickManager.TicksGame % 60 == 0 && AnyHostilePawnNearby(base.Pawn.Position, base.Pawn.Map, Props.triggerRadius))
		{
			SpawnMechClusterStyleTurrets(base.Pawn);
			activated = true;
			base.Pawn.health.RemoveHediff(parent);
		}
	}

	private bool AnyHostilePawnNearby(IntVec3 center, Map map, float radius)
	{
		foreach (Pawn pawn in map.mapPawns.AllPawns)
		{
			if (pawn.HostileTo(base.Pawn) && pawn.Position.DistanceTo(center) <= radius)
			{
				return true;
			}
		}
		return false;
	}

	private void SpawnMechClusterStyleTurrets(Pawn mech)
	{
		if (mech?.Map == null)
		{
			Log.Error("SpawnMechClusterStyleTurrets was called with a null mech or map.");
			return;
		}
		ThingDef turretDef = Props.turretDef;
		if (turretDef == null)
		{
			Log.Error("HediffComp_TurretCallerAutoMortar: turretDef is not configured in XML");
			return;
		}
		Map map = mech.Map;
		int successfulSpawns = 0;
		Predicate<IntVec3> isValidCell = (IntVec3 c) => c.Walkable(map) && !map.roofGrid.Roofed(c) && c.GetFirstBuilding(map) == null && c.DistanceTo(mech.Position) >= (float)Props.minSpawnDistance && !(map.fogGrid?.IsFogged(c) ?? false);
		for (int i = 0; i < Props.turretCount; i++)
		{
			if (CellFinder.TryFindRandomCellNear(mech.Position, map, Props.spawnRadius, isValidCell, out var dropPos))
			{
				SpawnTurret(dropPos, map, mech.Faction, turretDef, Props.leaveSlag);
				successfulSpawns++;
				continue;
			}
			Log.Warning("Could not find a valid cell to spawn primary turret for " + mech.LabelShort + ". Aborting further spawns.");
			break;
		}
		if (Props.extraTurretDef != null && Props.extraTurretCount > 0)
		{
			for (int i2 = 0; i2 < Props.extraTurretCount; i2++)
			{
				if (CellFinder.TryFindRandomCellNear(mech.Position, map, Props.spawnRadius, isValidCell, out var dropPos2))
				{
					SpawnTurret(dropPos2, map, mech.Faction, Props.extraTurretDef, Props.extraTurretLeaveSlag);
					successfulSpawns++;
				}
			}
		}
		if (successfulSpawns == 0)
		{
			Messages.Message("Mortar call received no response due to lack of suitable drop points.".Translate(), MessageTypeDefOf.NegativeEvent);
		}
	}

	private void SpawnTurret(IntVec3 dropPos, Map map, Faction faction, ThingDef turretDef, bool leaveSlag = true)
	{
		try
		{
			Thing turret = ThingMaker.MakeThing(turretDef);
			turret.SetFaction(faction);
			DropPodUtility.DropThingsNear(dropPos, map, new List<Thing> { turret }, 110, canInstaDropDuringInit: false, leaveSlag, Props.canRoofPunch, forbid: false, allowFogged: true, faction);
			if (Current.ProgramState == ProgramState.Playing)
			{
				FleckMaker.ThrowSmoke(dropPos.ToVector3Shifted(), map, 1.5f);
				FleckMaker.ThrowDustPuff(dropPos, map, 1f);
			}
		}
		catch (Exception arg)
		{
			Log.Error($"Failed to spawn turret '{turretDef.defName}' at {dropPos}: {arg}");
		}
	}
}
