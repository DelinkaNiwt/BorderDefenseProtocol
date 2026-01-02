using System;
using System.Collections.Generic;
using System.Linq;
using AncotLibrary;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Milira;

[StaticConstructorOnStartup]
public class MiliraPawnFlyer_Lance : MiliraPawnFlyer
{
	private static readonly Func<float, float> FlightSpeed;

	private static readonly Material lanceEffect;

	public override bool drawEquipment => false;

	private ThingWithComps weapon => base.FlyingPawn.equipment.Primary;

	private QualityCategory quality => weapon.TryGetComp<CompQuality>().Quality;

	private float damageAmountBase => weapon.def.tools.First().power;

	private float armorPenetrationBase => weapon.def.tools.First().armorPenetration;

	private float damageAmount => AncotUtility.QualityFactor(quality) * damageAmountBase;

	private float armorPenetration => AncotUtility.QualityFactor(quality) * armorPenetrationBase;

	static MiliraPawnFlyer_Lance()
	{
		lanceEffect = MaterialPool.MatFrom("Milira/Effect/Lance_Effect", ShaderDatabase.MoteGlow, new Color(1f, 1f, 1f, 1f));
		AnimationCurve animationCurve = new AnimationCurve();
		animationCurve.AddKey(0f, 0f);
		animationCurve.AddKey(0.1f, -0.03f);
		animationCurve.AddKey(0.2f, -0.05f);
		animationCurve.AddKey(0.3f, -0.07f);
		animationCurve.AddKey(0.4f, -0.09f);
		animationCurve.AddKey(0.5f, -0.1f);
		animationCurve.AddKey(0.6f, -0.11f);
		animationCurve.AddKey(1f, 1f);
		FlightSpeed = animationCurve.Evaluate;
	}

	protected override void Tick()
	{
		if (base.FlyingPawn != null && (float)ticksFlying / (float)ticksFlightTime > 0.6f)
		{
			ChargeDamage();
		}
		base.Tick();
	}

	public override void RecomputePosition()
	{
		if (positionLastComputedTick != ticksFlying)
		{
			positionLastComputedTick = ticksFlying;
			float arg = (float)ticksFlying / (float)ticksFlightTime;
			float t = FlightSpeed(arg);
			effectiveHeight = 0f;
			groundPos = Vector3.LerpUnclamped(startVec, base.DestinationPos, t);
			Vector3 vector = new Vector3(0f, 0f, 2f);
			Vector3 vector2 = Altitudes.AltIncVect * effectiveHeight;
			Vector3 vector3 = vector * effectiveHeight;
			effectivePos = groundPos + vector2 + vector3;
		}
	}

	protected override void DrawAt(Vector3 drawLoc, bool flip = false)
	{
		base.DrawAt(drawLoc, flip);
		if (base.CarriedThing != null && base.FlyingPawn != null)
		{
			PawnRenderUtility.DrawCarriedThing(base.FlyingPawn, effectivePos, base.CarriedThing);
		}
		float aimAngle = base.direction.AngleFlat();
		if ((float)ticksFlying / (float)ticksFlightTime < 0.6f)
		{
			aimAngle = 0f;
		}
		if ((float)ticksFlying / (float)ticksFlightTime > 0.6f)
		{
			LanceEffect(drawLoc, aimAngle);
		}
	}

	public virtual void LanceEffect(Vector3 drawLoc, float aimAngle)
	{
		Mesh mesh = null;
		float num = aimAngle;
		mesh = ((aimAngle > 20f && aimAngle < 160f) ? MeshPool.plane10 : ((!(aimAngle > 200f) || !(aimAngle < 340f)) ? MeshPool.plane10 : MeshPool.plane10Flip));
		num %= 360f;
		Material material = lanceEffect;
		drawLoc.y = AltitudeLayer.PawnState.AltitudeFor();
		Matrix4x4 matrix = Matrix4x4.TRS(s: new Vector3(4f, 0f, 8f), pos: drawLoc, q: Quaternion.AngleAxis(num, Vector3.up));
		Graphics.DrawMesh(mesh, matrix, material, 0);
	}

	public override void LandingEffects()
	{
		base.LandingEffects();
	}

	private void ChargeDamage()
	{
		List<Thing> list = new List<Thing>();
		IntVec3 center = DrawPos.ToIntVec3();
		CellRect cellRect = CellRect.CenteredOn(center, 1);
		Map map = base.Map;
		foreach (IntVec3 item in cellRect)
		{
			list.AddRange(item.GetThingList(map));
		}
		if (list.Count <= 0)
		{
			return;
		}
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i] is Pawn)
			{
				Pawn pawn = list[i] as Pawn;
				if (pawn.Faction != base.FlyingPawn.Faction && !pawn.Downed)
				{
					AncotUtility.DoDamage((Thing)pawn, DamageDefOf.Stab, damageAmount, armorPenetration, (Thing)base.FlyingPawn);
				}
			}
			if (list[i] is Building)
			{
				Building building = list[i] as Building;
				if (building.Faction != base.FlyingPawn.Faction)
				{
					AncotUtility.DoDamage((Thing)building, DamageDefOf.Stab, damageAmount, armorPenetration, (Thing)base.FlyingPawn);
				}
			}
		}
	}

	public new static MiliraPawnFlyer_Lance MakeFlyer(ThingDef flyingDef, Pawn pawn, IntVec3 destCell, EffecterDef flightEffecterDef, SoundDef landingSound, bool flyWithCarriedThing = false, Vector3? overrideStartVec = null, Ability triggeringAbility = null, LocalTargetInfo target = default(LocalTargetInfo))
	{
		MiliraPawnFlyer_Lance miliraPawnFlyer_Lance = (MiliraPawnFlyer_Lance)ThingMaker.MakeThing(flyingDef);
		miliraPawnFlyer_Lance.startVec = overrideStartVec ?? pawn.TrueCenter();
		miliraPawnFlyer_Lance.Rotation = pawn.Rotation;
		miliraPawnFlyer_Lance.flightDistance = pawn.Position.DistanceTo(destCell);
		miliraPawnFlyer_Lance.destCell = destCell;
		miliraPawnFlyer_Lance.pawnWasDrafted = pawn.Drafted;
		miliraPawnFlyer_Lance.flightEffecterDef = flightEffecterDef;
		miliraPawnFlyer_Lance.soundLanding = landingSound;
		miliraPawnFlyer_Lance.triggeringAbility = triggeringAbility?.def;
		miliraPawnFlyer_Lance.target = target;
		if (pawn.drafter != null)
		{
			miliraPawnFlyer_Lance.pawnCanFireAtWill = pawn.drafter.FireAtWill;
		}
		if (pawn.CurJob != null)
		{
			if (pawn.CurJob.def == JobDefOf.CastJump)
			{
				pawn.jobs.EndCurrentJob(JobCondition.Succeeded);
			}
			else
			{
				pawn.jobs.SuspendCurrentJob(JobCondition.InterruptForced);
			}
		}
		miliraPawnFlyer_Lance.jobQueue = pawn.jobs.CaptureAndClearJobQueue();
		if (flyWithCarriedThing && pawn.carryTracker.CarriedThing != null && pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Direct, out miliraPawnFlyer_Lance.carriedThing))
		{
			if (miliraPawnFlyer_Lance.carriedThing.holdingOwner != null)
			{
				miliraPawnFlyer_Lance.carriedThing.holdingOwner.Remove(miliraPawnFlyer_Lance.carriedThing);
			}
			miliraPawnFlyer_Lance.carriedThing.DeSpawn();
		}
		if (pawn.Spawned)
		{
			pawn.DeSpawn(DestroyMode.WillReplace);
		}
		if (!miliraPawnFlyer_Lance.innerContainer.TryAdd(pawn))
		{
			Log.Error("Could not add " + pawn.ToStringSafe() + " to a flyer.");
			pawn.Destroy();
		}
		if (miliraPawnFlyer_Lance.carriedThing != null && !miliraPawnFlyer_Lance.innerContainer.TryAdd(miliraPawnFlyer_Lance.carriedThing))
		{
			Log.Error("Could not add " + miliraPawnFlyer_Lance.carriedThing.ToStringSafe() + " to a flyer.");
		}
		return miliraPawnFlyer_Lance;
	}
}
