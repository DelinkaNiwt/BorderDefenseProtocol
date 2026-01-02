using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public static class ForceMovementUtility
{
	public static void ForceMoveToTarget(IntVec3 target, Pawn pawn)
	{
		pawn.Position = target;
		pawn.pather.StopDead();
		pawn.jobs.StopAll();
	}

	public static IntVec3 GetDestinationAngle(Pawn victim, float distance, float angle, List<HediffDef> removeHediffsAffected = null, bool ignoreResistance = false, float fieldStrength = 1f)
	{
		Vector3 vector = Vector3Utility.HorizontalVectorFromAngle(angle);
		vector.Normalize();
		IntVec3 result = victim.Position;
		if (!ignoreResistance)
		{
			float statValue = victim.GetStatValue(AncotDefOf.Ancot_FieldForceResistance);
			distance = Math.Max(0f, distance * (fieldStrength - statValue));
		}
		for (int i = 1; (float)i <= distance; i++)
		{
			Vector3 vector2 = vector * i;
			IntVec3 intVec = victim.Position + new IntVec3(Mathf.RoundToInt(vector2.x), 0, Mathf.RoundToInt(vector2.z));
			if (!intVec.InBounds(victim.Map) || !intVec.Walkable(victim.Map) || !GenSight.LineOfSight(victim.Position, intVec, victim.Map, skipFirstCell: true))
			{
				break;
			}
			result = intVec;
		}
		return result;
	}

	public static void ApplyRepulsiveForce(IntVec3 origin, Pawn victim, float distance, List<HediffDef> removeHediffsAffected = null, bool ignoreResistance = false, float fieldStrength = 1f)
	{
		if (victim == null)
		{
			return;
		}
		Vector3 vector = (victim.Position - origin).ToVector3();
		vector.Normalize();
		IntVec3 position = victim.Position;
		if (!ignoreResistance)
		{
			distance = Math.Max(0f, distance * (fieldStrength - victim.GetStatValue(AncotDefOf.Ancot_FieldForceResistance)));
		}
		for (int i = 0; (float)i < distance; i++)
		{
			Vector3 vect = i * vector;
			IntVec3 intVec = victim.Position + vect.ToIntVec3();
			if (!intVec.InBounds(victim.Map) || !intVec.Walkable(victim.Map) || !GenSight.LineOfSight(origin, intVec, victim.Map))
			{
				break;
			}
			position = intVec;
		}
		if (!position.IsValid)
		{
			return;
		}
		victim.Position = position;
		if (!removeHediffsAffected.NullOrEmpty())
		{
			for (int j = 0; j < removeHediffsAffected.Count; j++)
			{
				Hediff firstHediffOfDef = victim.health.hediffSet.GetFirstHediffOfDef(removeHediffsAffected[j]);
				if (firstHediffOfDef != null)
				{
					victim.health.RemoveHediff(firstHediffOfDef);
				}
			}
		}
		victim.pather.StopDead();
		victim.jobs.StopAll();
	}

	public static void ApplyGravitationalForce(IntVec3 origin, Pawn victim, float distance, List<HediffDef> removeHediffsAffected = null, bool ignoreResistance = false, float fieldStrength = 1f)
	{
		Vector3 vector = (origin - victim.Position).ToVector3();
		vector.Normalize();
		IntVec3 position = victim.Position;
		if (!ignoreResistance)
		{
			distance = Math.Max(0f, distance * (fieldStrength - victim.GetStatValue(AncotDefOf.Ancot_FieldForceResistance)));
		}
		for (int i = 0; (float)i < distance; i++)
		{
			Vector3 vect = i * vector;
			IntVec3 intVec = victim.Position + vect.ToIntVec3();
			if (!intVec.InBounds(victim.Map) || !intVec.Walkable(victim.Map) || !GenSight.LineOfSight(victim.PositionHeld, intVec, victim.Map))
			{
				break;
			}
			if (intVec == origin)
			{
				position = origin;
				break;
			}
			position = intVec;
		}
		if (!position.IsValid)
		{
			return;
		}
		victim.Position = position;
		if (!removeHediffsAffected.NullOrEmpty())
		{
			for (int j = 0; j < removeHediffsAffected.Count; j++)
			{
				Hediff firstHediffOfDef = victim.health.hediffSet.GetFirstHediffOfDef(removeHediffsAffected[j]);
				if (firstHediffOfDef != null)
				{
					victim.health.RemoveHediff(firstHediffOfDef);
				}
			}
		}
		victim.pather.StopDead();
		victim.jobs.StopAll();
	}

	public static void ApplyRepulsiveForceArea(IntVec3 origin, Pawn caster, Map map, float range, float distance, List<HediffDef> removeHediffsAffected = null, bool ignoreResistance = false, float fieldStrength = 1f, bool applyAlly = false)
	{
		foreach (Thing item in GenRadial.RadialDistinctThingsAround(origin, map, range, useCenter: true))
		{
			if (item is Pawn { Downed: false } pawn && (pawn.Faction != caster.Faction || applyAlly))
			{
				ApplyRepulsiveForce(origin, pawn, distance, removeHediffsAffected, ignoreResistance, fieldStrength);
			}
		}
	}
}
