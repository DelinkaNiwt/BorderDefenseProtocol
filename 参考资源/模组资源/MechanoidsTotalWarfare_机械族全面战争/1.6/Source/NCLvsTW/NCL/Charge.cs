using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace NCL;

public class Charge : PawnFlyer
{
	private int positionLastComputedTick = -1;

	private Vector3 groundPos;

	private float angle = -1f;

	private IntVec3 lastPos;

	private HashSet<Thing> hurtedTargets = new HashSet<Thing>();

	private static List<Thing> targets = new List<Thing>();

	private SkillBehaviorMode _behaviorMode;

	protected override void Tick()
	{
		AttackTick();
		base.Tick();
	}

	private void AttackTick()
	{
		RecomputePosition();
		if (groundPos.ToIntVec3() != lastPos)
		{
			lastPos = groundPos.ToIntVec3();
			CheckAndApplyDamage(GenAttackCells.NineCells);
		}
	}

	protected override void RespawnPawn()
	{
		DefDatabase<EffecterDef>.GetNamed("NCL_Crack").Spawn(base.Position, base.Map);
		SoundDef.Named("Explosion_Bomb").PlayOneShot(new TargetInfo(base.Position, base.Map));
		CheckAndApplyDamage(GenAttackCells.TwentyFiveCells);
		SetBehaviorMode("ChargeB");
		base.RespawnPawn();
	}

	public void SetBehaviorMode(string modeId)
	{
		_behaviorMode = SkillGraphics.GetBehaviorMode(SkillType.Charge, modeId);
	}

	private void CheckAndApplyDamage(List<IntVec3> cellOffsets)
	{
		foreach (IntVec3 offset in cellOffsets)
		{
			IntVec3 cell = base.Position + offset;
			if (!cell.InBounds(base.Map))
			{
				continue;
			}
			List<Thing> things = new List<Thing>(cell.GetThingList(base.Map));
			foreach (Thing thing in things)
			{
				if (ShouldDamageTarget(thing))
				{
					ApplyDamage(thing);
				}
			}
		}
	}

	private bool ShouldDamageTarget(Thing target)
	{
		return base.FlyingPawn != null && !hurtedTargets.Contains(target) && target != base.FlyingPawn;
	}

	private void ApplyDamage(Thing target)
	{
		hurtedTargets.Add(target);
		if (target is Pawn pawn)
		{
			Vector3 direction = pawn.DrawPos - groundPos;
			Hediff_BeatenOff.BeatOff(pawn, direction, 6f);
			DamageInfo damage = new DamageInfo(DamageDefOf.Blunt, 40f, 0.88f, direction.AngleFlat(), base.FlyingPawn);
			pawn.TakeDamage(damage);
		}
	}

	private void RecomputePosition()
	{
		if (positionLastComputedTick != ticksFlying)
		{
			if (angle < 0f)
			{
				angle = (base.DestinationPos - startVec).AngleFlat();
			}
			positionLastComputedTick = ticksFlying;
			float progress = (float)ticksFlying / (float)ticksFlightTime;
			groundPos = Vector3.Lerp(startVec, base.DestinationPos, progress);
		}
	}

	public override void DynamicDrawPhaseAt(DrawPhase phase, Vector3 drawLoc, bool flip = false)
	{
		RecomputePosition();
		if (_behaviorMode?.graphicDataA != null)
		{
			Graphic graphic = _behaviorMode.graphicDataA.Graphic;
			Mesh mesh = ((angle > 180f) ? MeshPool.GridPlaneFlip(graphic.drawSize) : MeshPool.GridPlane(graphic.drawSize));
			Graphics.DrawMesh(mesh, groundPos, Quaternion.AngleAxis(0f, Vector3.up), graphic.MatSingle, 0);
		}
		else if (base.FlyingPawn != null)
		{
			base.DynamicDrawPhaseAt(phase, groundPos, flip);
		}
	}

	public static Charge Make(ThingDef flyingDef, Ability ability, Pawn pawn, IntVec3 destCell, EffecterDef flightEffecterDef, SoundDef landingSound, bool flyWithCarriedThing)
	{
		pawn.rotationTracker.FaceCell(destCell);
		return PawnFlyer.MakeFlyer(flyingDef, pawn, destCell, flightEffecterDef, landingSound, flyWithCarriedThing) as Charge;
	}
}
