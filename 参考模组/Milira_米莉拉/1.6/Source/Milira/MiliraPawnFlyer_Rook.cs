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
public class MiliraPawnFlyer_Rook : MiliraPawnFlyer
{
	public static float knockBackDistance;

	private static readonly Func<float, float> FlightSpeed;

	private static readonly Material shieldEffect;

	public override bool drawEquipment => false;

	private ThingWithComps weapon => (base.FlyingPawn.equipment.Primary != null) ? base.FlyingPawn.equipment.Primary : base.FlyingPawn;

	private QualityCategory quality
	{
		get
		{
			if (weapon.TryGetComp<CompQuality>() != null)
			{
				return weapon.TryGetComp<CompQuality>().Quality;
			}
			return QualityCategory.Normal;
		}
	}

	public VerbTracker verbTracker => (base.FlyingPawn.equipment.Primary == null || !base.FlyingPawn.equipment.Primary.def.IsMeleeWeapon) ? base.FlyingPawn.verbTracker : weapon.TryGetComp<CompEquippable>()?.verbTracker;

	private float damageAmount => verbTracker.AllVerbs.First().verbProps.AdjustedMeleeDamageAmount(verbTracker.AllVerbs.First(), base.FlyingPawn);

	private float armorPenetration => verbTracker.AllVerbs.First().verbProps.AdjustedArmorPenetration(verbTracker.AllVerbs.First(), base.FlyingPawn);

	static MiliraPawnFlyer_Rook()
	{
		knockBackDistance = 8f;
		shieldEffect = MaterialPool.MatFrom("Milira/Effect/RookShield_Effect", ShaderDatabase.MoteGlow, new Color(1f, 1f, 1f, 1f));
		AnimationCurve animationCurve = new AnimationCurve();
		animationCurve.AddKey(0f, 0f);
		animationCurve.AddKey(0.1f, 0f);
		animationCurve.AddKey(0.2f, 0f);
		animationCurve.AddKey(0.3f, 0f);
		animationCurve.AddKey(0.4f, 0f);
		animationCurve.AddKey(1f, 1f);
		FlightSpeed = animationCurve.Evaluate;
	}

	protected override void Tick()
	{
		if (base.FlyingPawn != null && (float)ticksFlying / (float)ticksFlightTime > 0.4f)
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
		if ((float)ticksFlying / (float)ticksFlightTime > 0.4f)
		{
			FlyingEffect(drawLoc, aimAngle);
		}
	}

	public virtual void FlyingEffect(Vector3 drawLoc, float aimAngle)
	{
		Mesh mesh = null;
		if (aimAngle > 20f && aimAngle < 160f)
		{
			mesh = MeshPool.plane10;
		}
		else if (aimAngle > 200f && aimAngle < 340f)
		{
			mesh = MeshPool.plane10Flip;
		}
		else
		{
			mesh = MeshPool.plane10;
		}
	}

	public override void LandingEffects()
	{
		base.LandingEffects();
	}

	private void ChargeDamage()
	{
		List<Thing> list = new List<Thing>();
		IntVec3 intVec = DrawPos.ToIntVec3();
		CellRect cellRect = CellRect.CenteredOn(intVec, 2);
		Map map = base.Map;
		foreach (IntVec3 item in cellRect)
		{
			if (item.InBounds(map))
			{
				list.AddRange(item.GetThingList(map));
			}
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
					if (!pawn.Dead)
					{
						IntVec3 position = MiliraPawnFlyer_Hammer.TargetPosition(intVec, pawn, knockBackDistance);
						pawn.Position = position;
						pawn.pather.StopDead();
						pawn.jobs.StopAll();
					}
					break;
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

	public new static MiliraPawnFlyer_Rook MakeFlyer(ThingDef flyingDef, Pawn pawn, IntVec3 destCell, EffecterDef flightEffecterDef, SoundDef landingSound, bool flyWithCarriedThing = false, Vector3? overrideStartVec = null, Ability triggeringAbility = null, LocalTargetInfo target = default(LocalTargetInfo))
	{
		MiliraPawnFlyer_Rook miliraPawnFlyer_Rook = (MiliraPawnFlyer_Rook)ThingMaker.MakeThing(flyingDef);
		miliraPawnFlyer_Rook.startVec = overrideStartVec ?? pawn.TrueCenter();
		miliraPawnFlyer_Rook.Rotation = pawn.Rotation;
		miliraPawnFlyer_Rook.flightDistance = pawn.Position.DistanceTo(destCell);
		miliraPawnFlyer_Rook.destCell = destCell;
		miliraPawnFlyer_Rook.pawnWasDrafted = pawn.Drafted;
		miliraPawnFlyer_Rook.flightEffecterDef = flightEffecterDef;
		miliraPawnFlyer_Rook.soundLanding = landingSound;
		miliraPawnFlyer_Rook.triggeringAbility = triggeringAbility?.def;
		miliraPawnFlyer_Rook.target = target;
		if (pawn.drafter != null)
		{
			miliraPawnFlyer_Rook.pawnCanFireAtWill = pawn.drafter.FireAtWill;
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
		miliraPawnFlyer_Rook.jobQueue = pawn.jobs.CaptureAndClearJobQueue();
		if (flyWithCarriedThing && pawn.carryTracker.CarriedThing != null && pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Direct, out miliraPawnFlyer_Rook.carriedThing))
		{
			if (miliraPawnFlyer_Rook.carriedThing.holdingOwner != null)
			{
				miliraPawnFlyer_Rook.carriedThing.holdingOwner.Remove(miliraPawnFlyer_Rook.carriedThing);
			}
			miliraPawnFlyer_Rook.carriedThing.DeSpawn();
		}
		if (pawn.Spawned)
		{
			pawn.DeSpawn(DestroyMode.WillReplace);
		}
		if (!miliraPawnFlyer_Rook.innerContainer.TryAdd(pawn))
		{
			Log.Error("Could not add " + pawn.ToStringSafe() + " to a flyer.");
			pawn.Destroy();
		}
		if (miliraPawnFlyer_Rook.carriedThing != null && !miliraPawnFlyer_Rook.innerContainer.TryAdd(miliraPawnFlyer_Rook.carriedThing))
		{
			Log.Error("Could not add " + miliraPawnFlyer_Rook.carriedThing.ToStringSafe() + " to a flyer.");
		}
		return miliraPawnFlyer_Rook;
	}
}
