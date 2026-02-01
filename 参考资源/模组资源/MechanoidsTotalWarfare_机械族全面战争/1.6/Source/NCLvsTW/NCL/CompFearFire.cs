using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace NCL;

public class CompFearFire : ThingComp
{
	private int nextCheckTick = 0;

	private int panicEndTick = 0;

	private int lastYellTick = 0;

	private int lastDebugDrawTick = 0;

	private List<IntVec3> lastDetectedFires = new List<IntVec3>();

	private CompProperties_FearFire Props => (CompProperties_FearFire)props;

	public bool IsPanicking => Find.TickManager.TicksGame < panicEndTick;

	public override void CompTick()
	{
		base.CompTick();
		if (!(parent is Pawn { Spawned: not false, Map: not null, Dead: false, Downed: false } pawn))
		{
			return;
		}
		int currentTick = Find.TickManager.TicksGame;
		if (Props.showDebugRadius && currentTick - lastDebugDrawTick > 60)
		{
			DebugDrawDetectionRadius();
			lastDebugDrawTick = currentTick;
		}
		if (currentTick >= nextCheckTick)
		{
			nextCheckTick = currentTick + Props.checkInterval;
			CheckForFire(pawn);
		}
		if (IsPanicking && Props.showVisualEffects && currentTick % 30 == 0)
		{
			FleckMaker.ThrowAirPuffUp(pawn.DrawPos, pawn.Map);
			if (Props.showPanicText && currentTick - lastYellTick > 120 && pawn.health.capacities.CapableOf(PawnCapacityDefOf.Talking) && Rand.Value < Props.yellChance)
			{
				MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Σ(°△°|||)\ufe34".Translate(), Color.red, 2f);
				lastYellTick = currentTick;
			}
		}
	}

	private void DebugDrawDetectionRadius()
	{
		if (!parent.Spawned)
		{
			return;
		}
		GenDraw.DrawRadiusRing(parent.Position, Props.detectionRadius, IsPanicking ? Color.red : Color.yellow);
		foreach (IntVec3 lastDetectedFire in lastDetectedFires)
		{
			GenDraw.DrawCircleOutline(lastDetectedFire.ToVector3Shifted(), 0.5f);
		}
	}

	private void CheckForFire(Pawn pawn)
	{
		if (IsPanicking)
		{
			if (IsSafeFromFire(pawn.Position, pawn.Map))
			{
				panicEndTick = 0;
			}
		}
		else if (HasFireNearby(pawn.Position, pawn.Map))
		{
			TriggerPanic(pawn);
		}
	}

	private bool HasFireNearby(IntVec3 position, Map map)
	{
		lastDetectedFires.Clear();
		float radiusSq = Props.detectionRadius * Props.detectionRadius;
		foreach (IntVec3 cell in GenRadial.RadialCellsAround(position, Props.detectionRadius, useCenter: true))
		{
			if (cell.InBounds(map))
			{
				float distSq = (cell - position).LengthHorizontalSquared;
				if (!(distSq > radiusSq) && CellHasFire(cell, map))
				{
					lastDetectedFires.Add(cell);
					return true;
				}
			}
		}
		return false;
	}

	private List<IntVec3> GetNearbyFires(IntVec3 position, Map map)
	{
		List<IntVec3> fires = new List<IntVec3>();
		float radiusSq = Props.detectionRadius * Props.detectionRadius;
		foreach (IntVec3 cell in GenRadial.RadialCellsAround(position, Props.detectionRadius, useCenter: true))
		{
			if (cell.InBounds(map))
			{
				float distSq = (cell - position).LengthHorizontalSquared;
				if (distSq <= radiusSq && CellHasFire(cell, map))
				{
					fires.Add(cell);
				}
			}
		}
		return fires;
	}

	private void TriggerPanic(Pawn pawn)
	{
		if (!ShouldAffectPawn(pawn))
		{
			return;
		}
		panicEndTick = Find.TickManager.TicksGame + 600;
		if (Props.showPanicText && pawn.health.capacities.CapableOf(PawnCapacityDefOf.Talking))
		{
			if (pawn.IsColonist)
			{
				Messages.Message(pawn.LabelShort + " is panicking due to fire!", pawn, MessageTypeDefOf.ThreatSmall);
			}
			MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Σ(°△°|||)\ufe34".Translate(), Color.red, 2f);
		}
		if (pawn.Faction == null || !pawn.Faction.IsPlayer)
		{
			if (TryFindSafePosition(pawn, out var safePos))
			{
				FleeToPosition(pawn, safePos);
			}
			else
			{
				FleeRandomly(pawn);
			}
		}
	}

	private bool ShouldAffectPawn(Pawn pawn)
	{
		if (pawn.RaceProps.Humanlike)
		{
			return Props.affectHumans;
		}
		if (pawn.RaceProps.Animal)
		{
			return Props.affectAnimals;
		}
		if (pawn.RaceProps.IsMechanoid)
		{
			return Props.affectMechanoids;
		}
		return false;
	}

	private bool TryFindSafePosition(Pawn pawn, out IntVec3 safePos)
	{
		Map map = pawn.Map;
		safePos = IntVec3.Invalid;
		Vector3 escapeVector = CalculateFireEscapeDirection(pawn.Position, map);
		IntVec3 targetPos = pawn.Position + (escapeVector * Props.fleeDistance).ToIntVec3();
		Predicate<IntVec3> validator = (IntVec3 c) => c.InBounds(map) && IsSafeCell(c, map) && pawn.CanReach(c, PathEndMode.OnCell, Danger.Deadly);
		return CellFinder.TryFindRandomCellNear(targetPos, map, Mathf.RoundToInt(Props.fleeDistance * 0.5f), validator, out safePos);
	}

	private Vector3 CalculateFireEscapeDirection(IntVec3 position, Map map)
	{
		List<IntVec3> nearbyFires = GetNearbyFires(position, map);
		if (nearbyFires.Count == 0)
		{
			return RandomDirection();
		}
		Vector3 fireCenter = Vector3.zero;
		float totalWeight = 0f;
		foreach (IntVec3 firePos in nearbyFires)
		{
			float distance = Mathf.Max(0.1f, (firePos - position).LengthHorizontal);
			float weight = 1f / (distance * distance);
			fireCenter += firePos.ToVector3() * weight;
			totalWeight += weight;
		}
		fireCenter /= totalWeight;
		return (position.ToVector3() - fireCenter).normalized;
	}

	private Vector3 RandomDirection()
	{
		float angle = Rand.Range(0, 360);
		return Quaternion.AngleAxis(angle, Vector3.up) * Vector3.forward;
	}

	private bool IsSafeCell(IntVec3 cell, Map map)
	{
		if (CellHasFire(cell, map))
		{
			return false;
		}
		IntVec3[] adjacentCells = GenAdj.AdjacentCells;
		foreach (IntVec3 adjacentCell in adjacentCells)
		{
			IntVec3 nearbyCell = cell + adjacentCell;
			if (nearbyCell.InBounds(map) && CellHasFire(nearbyCell, map))
			{
				return false;
			}
		}
		return true;
	}

	private bool IsSafeFromFire(IntVec3 position, Map map)
	{
		if (CellHasFire(position, map))
		{
			return false;
		}
		IntVec3[] adjacentCells = GenAdj.AdjacentCells;
		foreach (IntVec3 adjacentCell in adjacentCells)
		{
			IntVec3 cell = position + adjacentCell;
			if (cell.InBounds(map) && CellHasFire(cell, map))
			{
				return false;
			}
		}
		return true;
	}

	private bool CellHasFire(IntVec3 cell, Map map)
	{
		if (!cell.InBounds(map))
		{
			return false;
		}
		List<Thing> things = map.thingGrid.ThingsListAt(cell);
		if (things == null || things.Count == 0)
		{
			return false;
		}
		for (int i = 0; i < things.Count; i++)
		{
			if (things[i].def == ThingDefOf.Fire)
			{
				return true;
			}
		}
		return false;
	}

	private void FleeToPosition(Pawn pawn, IntVec3 targetPos)
	{
		if (pawn.CurJob != null && pawn.CurJob.def == JobDefOf.ExtinguishSelf)
		{
			return;
		}
		Job fleeJob = JobMaker.MakeJob(JobDefOf.Flee, targetPos);
		fleeJob.locomotionUrgency = LocomotionUrgency.Sprint;
		fleeJob.expiryInterval = 600;
		fleeJob.checkOverrideOnExpire = true;
		pawn.jobs.StartJob(fleeJob, JobCondition.InterruptForced);
		try
		{
			pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.PanicFlee);
		}
		catch
		{
		}
	}

	private void FleeRandomly(Pawn pawn)
	{
		if (RCellFinder.TryFindRandomExitSpot(pawn, out var fleeTarget))
		{
			FleeToPosition(pawn, fleeTarget);
			return;
		}
		Vector3 escapeDir = RandomDirection();
		IntVec3 targetPos = pawn.Position + (escapeDir * Props.fleeDistance).ToIntVec3();
		if (targetPos.InBounds(pawn.Map))
		{
			FleeToPosition(pawn, targetPos);
		}
	}
}
