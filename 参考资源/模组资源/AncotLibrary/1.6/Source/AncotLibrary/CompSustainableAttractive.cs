using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public class CompSustainableAttractive : ThingComp
{
	private static readonly SimpleCurve DistanceToPulldistanceFactor = new SimpleCurve
	{
		new CurvePoint(0f, 6f),
		new CurvePoint(30f, 3f)
	};

	private CompProperties_SustainableAttractive Props => (CompProperties_SustainableAttractive)props;

	public override void CompTickInterval(int delta)
	{
		base.CompTickInterval(delta);
		if (!parent.IsHashIntervalTick(Props.intervalTick, delta))
		{
			return;
		}
		foreach (Pawn item in parent.Map.mapPawns.AllPawnsSpawned)
		{
			if (IsPawnCanPull(item))
			{
				TryToPullIn(parent, item, Props.distance);
			}
		}
		foreach (Pawn item2 in parent.Map.mapPawns.AllPawnsSpawned)
		{
			if (IsPawnDamaged(item2))
			{
				AncotUtility.DoDamage(item2, DamageDefOf.Crush, 10f, 0.5f);
			}
		}
	}

	private bool IsPawnCanPull(Pawn target)
	{
		if (target.Dead || target.health == null || target.Downed || !target.Spawned)
		{
			return false;
		}
		if (target.Position.DistanceTo(parent.Position) <= Props.range)
		{
			return true;
		}
		return false;
	}

	private bool IsPawnDamaged(Pawn target)
	{
		if (target.Dead || target.health == null || target.Downed || !target.Spawned)
		{
			return false;
		}
		if (target.Position.DistanceTo(parent.Position) <= Props.damageRange)
		{
			return true;
		}
		return false;
	}

	public void TryToPullIn(Thing trap, Thing thing, float pullDistance)
	{
		IntVec3 intVec = trap.PositionHeld - thing.Position;
		float num = DistanceToPulldistanceFactor.Evaluate(intVec.Magnitude);
		Vector3 vector = intVec.ToVector3();
		vector.Normalize();
		IntVec3 position = thing.Position;
		for (int i = 0; (float)i < num; i++)
		{
			Vector3 vect = i * vector;
			IntVec3 intVec2 = thing.Position + vect.ToIntVec3();
			if (!intVec2.InBounds(thing.Map) || !intVec2.Walkable(thing.Map) || !GenSight.LineOfSight(trap.PositionHeld, intVec2, thing.Map))
			{
				break;
			}
			if (intVec2 == trap.PositionHeld)
			{
				position = trap.PositionHeld;
				break;
			}
			position = intVec2;
		}
		if (position.IsValid)
		{
			thing.Position = position;
			if (thing is Pawn pawn)
			{
				pawn.pather.StopDead();
				pawn.jobs.StopAll();
			}
		}
	}
}
