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
public class MiliraPawnFlyer_KnightCharge : MiliraPawnFlyer
{
	private static readonly Func<float, float> FlightSpeed;

	private float damageAmountExact = 0f;

	private bool startedCharge;

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

	private DamageDef damageDef => weapon.def.tools.First().capacities.First().Maneuvers.First().verb.meleeDamageDef ?? DamageDefOf.Stab;

	static MiliraPawnFlyer_KnightCharge()
	{
		AnimationCurve animationCurve = new AnimationCurve();
		animationCurve.AddKey(0f, 0f);
		animationCurve.AddKey(0.1f, 0.3f);
		animationCurve.AddKey(0.2f, 0.47f);
		animationCurve.AddKey(0.3f, 0.6f);
		animationCurve.AddKey(0.4f, 0.69f);
		animationCurve.AddKey(0.5f, 0.77f);
		animationCurve.AddKey(0.6f, 0.84f);
		animationCurve.AddKey(0.7f, 0.9f);
		animationCurve.AddKey(0.8f, 0.95f);
		animationCurve.AddKey(0.9f, 0.98f);
		animationCurve.AddKey(1f, 1f);
		FlightSpeed = animationCurve.Evaluate;
	}

	protected override void Tick()
	{
		if (base.FlyingPawn != null)
		{
			if (!startedCharge)
			{
				damageAmountExact = damageAmount;
				startedCharge = true;
			}
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
	}

	public override void LandingEffects()
	{
		base.LandingEffects();
		if (ModsConfig.IsActive("Ancot.MilianModification") && base.FlyingPawn.health.hediffSet.GetFirstHediffOfDef(MiliraDefOf.MilianFitting_CoordinatedBooster) != null)
		{
			HealthUtility.AdjustSeverity(base.FlyingPawn, MiliraDefOf.Milian_CoordinatedBooster, 1f);
		}
		startedCharge = false;
	}

	private void ChargeDamage()
	{
		List<Thing> list = new List<Thing>();
		IntVec3 c = DrawPos.ToIntVec3();
		Map map = base.Map;
		list.AddRange(c.GetThingList(map));
		if (list.Count <= 0)
		{
			return;
		}
		for (int i = 0; i < list.Count; i++)
		{
			damageAmountExact = Math.Max(damageAmountExact * 0.9f, damageAmount * 0.3f);
			if (list[i] is Pawn)
			{
				Pawn pawn = list[i] as Pawn;
				if (pawn.Faction != base.FlyingPawn.Faction && !pawn.Downed)
				{
					AncotUtility.DoDamage((Thing)pawn, damageDef, damageAmountExact, armorPenetration, (Thing)base.FlyingPawn);
					break;
				}
			}
			if (list[i] is Building)
			{
				Building building = list[i] as Building;
				if (building.Faction != base.FlyingPawn.Faction)
				{
					AncotUtility.DoDamage((Thing)building, damageDef, damageAmountExact / 5f, armorPenetration, (Thing)base.FlyingPawn);
				}
			}
		}
	}

	public new static MiliraPawnFlyer_KnightCharge MakeFlyer(ThingDef flyingDef, Pawn pawn, IntVec3 destCell, EffecterDef flightEffecterDef, SoundDef landingSound, bool flyWithCarriedThing = false, Vector3? overrideStartVec = null, Ability triggeringAbility = null, LocalTargetInfo target = default(LocalTargetInfo))
	{
		MiliraPawnFlyer_KnightCharge miliraPawnFlyer_KnightCharge = (MiliraPawnFlyer_KnightCharge)ThingMaker.MakeThing(flyingDef);
		miliraPawnFlyer_KnightCharge.startVec = overrideStartVec ?? pawn.TrueCenter();
		miliraPawnFlyer_KnightCharge.Rotation = pawn.Rotation;
		miliraPawnFlyer_KnightCharge.flightDistance = pawn.Position.DistanceTo(destCell);
		miliraPawnFlyer_KnightCharge.destCell = destCell;
		miliraPawnFlyer_KnightCharge.pawnWasDrafted = pawn.Drafted;
		miliraPawnFlyer_KnightCharge.flightEffecterDef = flightEffecterDef;
		miliraPawnFlyer_KnightCharge.soundLanding = landingSound;
		miliraPawnFlyer_KnightCharge.triggeringAbility = triggeringAbility?.def;
		miliraPawnFlyer_KnightCharge.target = target;
		if (pawn.drafter != null)
		{
			miliraPawnFlyer_KnightCharge.pawnCanFireAtWill = pawn.drafter.FireAtWill;
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
		miliraPawnFlyer_KnightCharge.jobQueue = pawn.jobs.CaptureAndClearJobQueue();
		if (flyWithCarriedThing && pawn.carryTracker.CarriedThing != null && pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Direct, out miliraPawnFlyer_KnightCharge.carriedThing))
		{
			if (miliraPawnFlyer_KnightCharge.carriedThing.holdingOwner != null)
			{
				miliraPawnFlyer_KnightCharge.carriedThing.holdingOwner.Remove(miliraPawnFlyer_KnightCharge.carriedThing);
			}
			miliraPawnFlyer_KnightCharge.carriedThing.DeSpawn();
		}
		if (pawn.Spawned)
		{
			pawn.DeSpawn(DestroyMode.WillReplace);
		}
		if (!miliraPawnFlyer_KnightCharge.innerContainer.TryAdd(pawn))
		{
			Log.Error("Could not add " + pawn.ToStringSafe() + " to a flyer.");
			pawn.Destroy();
		}
		if (miliraPawnFlyer_KnightCharge.carriedThing != null && !miliraPawnFlyer_KnightCharge.innerContainer.TryAdd(miliraPawnFlyer_KnightCharge.carriedThing))
		{
			Log.Error("Could not add " + miliraPawnFlyer_KnightCharge.carriedThing.ToStringSafe() + " to a flyer.");
		}
		return miliraPawnFlyer_KnightCharge;
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref damageAmountExact, "damageAmountExact", 0f);
		Scribe_Values.Look(ref startedCharge, "startedCharge", defaultValue: false);
	}
}
