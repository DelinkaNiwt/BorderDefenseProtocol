using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace NCL;

public class FlyingKick : PawnFlyer
{
	private IntVec3 _lastCell;

	private Vector3 _lastPos;

	private Vector3 _newPos;

	private const float LandingDistanceThreshold = 2.5f;

	private const float DamageAmount = 30f;

	private const float DamageArmorPenetration = 1.5f;

	private const float BeatOffDistance = 6f;

	private const float BeatOffSpeed = 10f;

	private const float EffectRadius = 4f;

	private SkillBehaviorMode _behaviorMode;

	private Vector3 GroundPos => base.DestinationPos;

	protected override void Tick()
	{
		if (DrawPos != _newPos)
		{
			_lastPos = _newPos;
			_newPos = DrawPos;
		}
		AttackTick();
		base.Tick();
	}

	protected override void RespawnPawn()
	{
		DefDatabase<EffecterDef>.GetNamed("NCL_Crack").Spawn(base.Position, base.Map);
		SoundDef.Named("Explosion_Bomb").PlayOneShot(new TargetInfo(base.Position, base.Map));
		Pawn flyingPawn = base.FlyingPawn;
		if (flyingPawn == null)
		{
			return;
		}
		List<Thing> targets = new List<Thing>();
		int cellsCount = GenRadial.NumCellsInRadius(4f);
		for (int i = 0; i < cellsCount; i++)
		{
			IntVec3 cell = base.Position + GenRadial.RadialPattern[i];
			if (!cell.InBounds(base.Map))
			{
				continue;
			}
			foreach (Thing thing in cell.GetThingList(base.Map))
			{
				if (IsValidTarget(thing))
				{
					targets.Add(thing);
				}
			}
		}
		foreach (Thing target in targets)
		{
			if (target is Pawn { Destroyed: false, Spawned: not false } pawn)
			{
				ApplyDamage(pawn, flyingPawn);
			}
		}
		base.RespawnPawn();
		FleckMaker.AttachedOverlay(flyingPawn, DefDatabase<FleckDef>.GetNamed("NCL_Stump_ShockWave"), Vector3.zero);
	}

	private void AttackTick()
	{
		if (!(base.Position != _lastCell))
		{
			return;
		}
		_lastCell = base.Position;
		List<Thing> targets = new List<Thing>();
		HashSet<Thing> localHurtedTargets = new HashSet<Thing>();
		IntVec3[] nineCellsLocal = GenAttackCells.NineCellsLocal;
		foreach (IntVec3 intVec in nineCellsLocal)
		{
			IntVec3 cell = intVec + _lastCell;
			if (!cell.InBounds(base.Map))
			{
				continue;
			}
			foreach (Thing thing in cell.GetThingList(base.Map))
			{
				if (IsValidTarget(thing) && !localHurtedTargets.Contains(thing))
				{
					localHurtedTargets.Add(thing);
					targets.Add(thing);
				}
			}
		}
		foreach (Thing target in targets)
		{
			if (target is Pawn { Destroyed: false, Spawned: not false } pawn)
			{
				ApplyDamage(pawn, base.FlyingPawn);
			}
		}
	}

	private bool IsValidTarget(Thing thing)
	{
		if (thing == null || base.FlyingPawn == null)
		{
			return false;
		}
		return (base.FlyingPawn.Faction == null) ? (thing != base.FlyingPawn) : base.FlyingPawn.HostileTo(thing);
	}

	private void ApplyDamage(Pawn targetPawn, Pawn instigator)
	{
		try
		{
			Vector3 vector = targetPawn.DrawPos - GroundPos;
			Hediff_BeatenOff.BeatOff(targetPawn, vector, 6f);
			targetPawn.TakeDamage(new DamageInfo(DamageDefOf.Blunt, 30f, 1.5f, vector.AngleFlat(), instigator));
		}
		catch (Exception arg)
		{
			Log.Error($"Error applying damage in FlyingKick: {arg}");
		}
	}

	public override void DynamicDrawPhaseAt(DrawPhase phase, Vector3 drawLoc, bool flip = false)
	{
		try
		{
			float angle = (_newPos - _lastPos).AngleFlat();
			bool isLanding = (base.DestinationPos - DrawPos).sqrMagnitude < 2.5f;
			_behaviorMode = SkillGraphics.GetBehaviorMode(SkillType.FlyingKick, isLanding ? "FlyingKickB" : "FlyingKickA");
			GraphicData graphicData = (isLanding ? _behaviorMode.graphicDataB : _behaviorMode.graphicDataA);
			if (graphicData != null)
			{
				Graphic graphic = graphicData.Graphic;
				Quaternion rotation = Quaternion.AngleAxis(isLanding ? 0f : angle, Vector3.up);
				Mesh mesh = ((angle > 180f) ? MeshPool.GridPlaneFlip(graphic.drawSize) : MeshPool.GridPlane(graphic.drawSize));
				Graphics.DrawMesh(mesh, DrawPos, rotation, graphic.MatSingle, 0);
			}
		}
		catch (Exception arg)
		{
			Log.Error($"Error in FlyingKick drawing: {arg}");
		}
	}

	public static FlyingKick Make(ThingDef flyingDef, Ability ability, Pawn pawn, IntVec3 destCell)
	{
		pawn.rotationTracker.FaceCell(destCell);
		return PawnFlyer.MakeFlyer(flyingDef, pawn, destCell, null, null) as FlyingKick;
	}
}
