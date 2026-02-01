using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace NCL;

public class Hediff_BeatenOff : Hediff
{
	private int _flyingTime;

	private int _maxFlyingTime;

	private Vector3 _startPos;

	private Vector3 _direction;

	private float _speed;

	public Vector3 DrawPosOverride => _startPos + _direction * _speed * _flyingTime / 60f;

	public override void Tick()
	{
		try
		{
			if (_flyingTime >= _maxFlyingTime || !pawn.Spawned || pawn.Dead)
			{
				RemoveHediff();
				return;
			}
			_flyingTime++;
			IntVec3 targetCell = DrawPosOverride.ToIntVec3();
			if (!targetCell.InBounds(pawn.Map) || targetCell.Impassable(pawn.Map))
			{
				ApplyCollisionDamage();
				RemoveHediff();
			}
			else if (pawn.Position != targetCell)
			{
				pawn.Position = targetCell;
				pawn.pather.Notify_Teleported_Int();
			}
		}
		catch (Exception arg)
		{
			Log.Error($"Error in Hediff_BeatenOff.Tick: {arg}");
			RemoveHediff();
		}
	}

	private void RemoveHediff()
	{
		pawn.pather.Notify_Teleported_Int();
		pawn.health.RemoveHediff(this);
	}

	private void ApplyCollisionDamage()
	{
		int hitCount = Rand.RangeInclusive(4, 10);
		float damagePerHit = 50f / (float)hitCount;
		for (int i = 0; i < hitCount; i++)
		{
			if (!pawn.Spawned)
			{
				break;
			}
			if (pawn.Dead)
			{
				break;
			}
			pawn.pather.Notify_Teleported_Int();
			pawn.TakeDamage(new DamageInfo(DamageDefOf.Blunt, damagePerHit, 1.5f, _direction.AngleFlat()));
		}
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref _flyingTime, "_flyingTime", 0);
		Scribe_Values.Look(ref _maxFlyingTime, "_maxFlyingTime", 0);
		Scribe_Values.Look(ref _startPos, "_startPos");
		Scribe_Values.Look(ref _direction, "_direction");
		Scribe_Values.Look(ref _speed, "_speed", 0f);
	}

	public static void BeatOff(Pawn target, Pawn instigator, float distance, float speed = 10f)
	{
		BeatOff(target, target.DrawPos - instigator.DrawPos, distance, speed);
	}

	public static void BeatOff(Pawn pawn, Vector3 direction, float distance, float speed = 10f)
	{
		try
		{
			if (pawn != null && !pawn.Destroyed && pawn.Spawned && !pawn.Dead && !pawn.health.hediffSet.HasHediff(HediffDef.Named("NCL_BeatenOff")) && HediffMaker.MakeHediff(HediffDef.Named("NCL_BeatenOff"), pawn) is Hediff_BeatenOff hediff)
			{
				hediff._startPos = pawn.TrueCenter();
				hediff._direction = direction.normalized;
				hediff._speed = speed;
				hediff._maxFlyingTime = Mathf.RoundToInt(distance / speed * 60f);
				pawn.health.AddHediff(hediff);
			}
		}
		catch (Exception arg)
		{
			Log.Error($"Error applying BeatOff: {arg}");
		}
	}
}
