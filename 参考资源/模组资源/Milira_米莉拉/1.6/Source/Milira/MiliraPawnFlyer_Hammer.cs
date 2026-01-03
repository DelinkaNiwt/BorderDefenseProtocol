using System;
using System.Collections.Generic;
using System.Linq;
using AncotLibrary;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace Milira;

[StaticConstructorOnStartup]
public class MiliraPawnFlyer_Hammer : MiliraPawnFlyer
{
	public static float knockBackDistance;

	private static readonly Func<float, float> FlightSpeed;

	private static readonly Func<float, float> FlightCurveHeight;

	private static readonly Func<float, float> FlightCurveHammerAngle;

	private static readonly Material hammerEffect;

	private List<IntVec3> tmpCells = new List<IntVec3>();

	public override bool drawEquipment => false;

	private ThingWithComps weapon => base.FlyingPawn.equipment.Primary;

	private QualityCategory quality => weapon.TryGetComp<CompQuality>().Quality;

	private float damageAmountBase => weapon.def.tools.First().power;

	private float armorPenetrationBase => weapon.def.tools.First().armorPenetration;

	private float damageAmount => AncotUtility.QualityFactor(quality) * damageAmountBase;

	private float armorPenetration => AncotUtility.QualityFactor(quality) * armorPenetrationBase;

	static MiliraPawnFlyer_Hammer()
	{
		knockBackDistance = 12f;
		hammerEffect = MaterialPool.MatFrom("Milira/Effect/Hammer_Effect", ShaderDatabase.MoteGlow, new Color(1f, 1f, 1f, 1f));
		AnimationCurve animationCurve = new AnimationCurve();
		animationCurve.AddKey(0f, 0f);
		animationCurve.AddKey(0.1f, -0.02f);
		animationCurve.AddKey(0.2f, -0.03f);
		animationCurve.AddKey(0.5f, 0f);
		animationCurve.AddKey(0.6f, 0f);
		animationCurve.AddKey(0.7f, 0f);
		animationCurve.AddKey(0.8f, 0.2f);
		animationCurve.AddKey(0.9f, 0.5f);
		animationCurve.AddKey(1f, 1f);
		FlightSpeed = animationCurve.Evaluate;
		AnimationCurve animationCurve2 = new AnimationCurve();
		animationCurve2.AddKey(0f, 0f);
		animationCurve2.AddKey(0.1f, 0.6f);
		animationCurve2.AddKey(0.2f, 1.1f);
		animationCurve2.AddKey(0.3f, 1.5f);
		animationCurve2.AddKey(0.4f, 1.8f);
		animationCurve2.AddKey(0.5f, 2f);
		animationCurve2.AddKey(0.6f, 2f);
		animationCurve2.AddKey(0.7f, 2f);
		animationCurve2.AddKey(0.8f, 1.9f);
		animationCurve2.AddKey(0.9f, 1.6f);
		animationCurve2.AddKey(1f, 0f);
		FlightCurveHeight = animationCurve2.Evaluate;
		AnimationCurve animationCurve3 = new AnimationCurve();
		animationCurve3.AddKey(0f, 90f);
		animationCurve3.AddKey(0.1f, 180f);
		animationCurve3.AddKey(0.2f, 240f);
		animationCurve3.AddKey(0.3f, 280f);
		animationCurve3.AddKey(0.4f, 305f);
		animationCurve3.AddKey(0.5f, 315f);
		animationCurve3.AddKey(0.6f, 315f);
		animationCurve3.AddKey(0.7f, 315f);
		animationCurve3.AddKey(0.8f, 330f);
		animationCurve3.AddKey(0.9f, 360f);
		animationCurve3.AddKey(1f, 420f);
		FlightCurveHammerAngle = animationCurve3.Evaluate;
	}

	public override void RecomputePosition()
	{
		if (positionLastComputedTick != ticksFlying)
		{
			positionLastComputedTick = ticksFlying;
			float arg = (float)ticksFlying / (float)ticksFlightTime;
			float t = FlightSpeed(arg);
			effectiveHeight = 2f * FlightCurveHeight(arg);
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
		float arg = (float)ticksFlying / (float)ticksFlightTime;
		float num = base.direction.AngleFlat();
		float num2 = (effectivePos - base.DestinationPos).AngleFlat();
		if (num > 0f && num <= 180f)
		{
			HammerEffect(drawLoc, FlightCurveHammerAngle(arg));
		}
		else
		{
			HammerEffect(drawLoc, 0f - FlightCurveHammerAngle(arg));
		}
	}

	public virtual void HammerEffect(Vector3 drawLoc, float aimAngle)
	{
		Mesh mesh = null;
		float num = aimAngle;
		mesh = ((aimAngle > 20f && aimAngle < 160f) ? MeshPool.plane10 : ((!(aimAngle > 200f) || !(aimAngle < 340f)) ? MeshPool.plane10 : MeshPool.plane10Flip));
		num %= 360f;
		Material material = hammerEffect;
		drawLoc.y = AltitudeLayer.PawnUnused.AltitudeFor();
		Matrix4x4 matrix = Matrix4x4.TRS(s: new Vector3(4f, 0f, 8f), pos: drawLoc, q: Quaternion.AngleAxis(num, Vector3.up));
		Graphics.DrawMesh(mesh, matrix, material, 0);
	}

	public override void LandingEffects()
	{
		List<Thing> list = new List<Thing>();
		IntVec3 intVec = base.DestinationPos.ToIntVec3();
		foreach (IntVec3 item in AffectedCells(intVec))
		{
			list.AddRange(item.GetThingList(base.Map));
			GenExplosion.DoExplosion(item, base.Map, 0.8f, MiliraDefOf.Milira_PlasmaBomb, base.FlyingPawn, (int)damageAmount, armorPenetration);
		}
		list.AddRange(intVec.GetThingList(base.Map));
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i] is Pawn)
			{
				Pawn pawn = list[i] as Pawn;
				if (pawn.Faction != base.FlyingPawn.Faction && !pawn.Downed)
				{
					AncotUtility.DoDamage((Thing)pawn, MiliraDefOf.Milira_PlasmaBomb, damageAmount, armorPenetration, (Thing)base.FlyingPawn);
					AncotUtility.DoDamage((Thing)pawn, DamageDefOf.Stun, damageAmount / 5f, armorPenetration, (Thing)base.FlyingPawn);
					if (!pawn.Dead)
					{
						IntVec3 position = TargetPosition(intVec, pawn, knockBackDistance);
						pawn.Position = position;
						pawn.pather.StopDead();
						pawn.jobs.StopAll();
					}
				}
			}
			if (list[i] is Building)
			{
				AncotUtility.DoDamage(list[i], MiliraDefOf.Milira_PlasmaBomb, 12f * damageAmount, armorPenetration);
			}
		}
		FleckMaker.Static(intVec, base.Map, MiliraDefOf.Milira_Building_TrapRepulsiveDistortion);
		MiliraDefOf.Milira_HammerSkill_Landing.PlayOneShot(new TargetInfo(base.Position, base.Map));
		base.LandingEffects();
	}

	private List<IntVec3> AffectedCells(IntVec3 target)
	{
		tmpCells.Clear();
		foreach (IntVec3 item in GenRadial.RadialCellsAround(target, 4f, useCenter: false))
		{
			if (item.IsValid || item.InBounds(base.Map))
			{
				tmpCells.Add(item);
			}
		}
		tmpCells = tmpCells.Distinct().ToList();
		tmpCells.RemoveAll((IntVec3 cell) => !CanUseCell(cell));
		return tmpCells;
		bool CanUseCell(IntVec3 c)
		{
			if (!c.InBounds(base.Map))
			{
				return false;
			}
			if (c == base.Position)
			{
				return false;
			}
			return true;
		}
	}

	public static IntVec3 TargetPosition(IntVec3 original, Pawn pawn2, float distance)
	{
		IntVec3 position = pawn2.Position;
		IntVec3 intVec = position - original;
		IntVec3 result = original;
		Vector3 vector = intVec.ToVector3();
		vector.Normalize();
		Map map = pawn2.Map;
		for (int i = 0; (float)i < distance; i++)
		{
			Vector3 vect = i * vector;
			IntVec3 intVec2 = original + vect.ToIntVec3();
			if (!ValidKnockBackTarget(map, intVec2))
			{
				break;
			}
			result = intVec2;
		}
		return result;
	}

	public static bool ValidKnockBackTarget(Map map, IntVec3 cell)
	{
		if (!cell.IsValid || !cell.InBounds(map))
		{
			return false;
		}
		if (cell.Impassable(map) || !cell.Walkable(map) || cell.Fogged(map))
		{
			return false;
		}
		Building edifice = cell.GetEdifice(map);
		if (edifice != null && edifice is Building_Door { Open: false })
		{
			return false;
		}
		return true;
	}

	public new static MiliraPawnFlyer_Hammer MakeFlyer(ThingDef flyingDef, Pawn pawn, IntVec3 destCell, EffecterDef flightEffecterDef, SoundDef landingSound, bool flyWithCarriedThing = false, Vector3? overrideStartVec = null, Ability triggeringAbility = null, LocalTargetInfo target = default(LocalTargetInfo))
	{
		MiliraPawnFlyer_Hammer miliraPawnFlyer_Hammer = (MiliraPawnFlyer_Hammer)ThingMaker.MakeThing(flyingDef);
		miliraPawnFlyer_Hammer.startVec = overrideStartVec ?? pawn.TrueCenter();
		miliraPawnFlyer_Hammer.Rotation = pawn.Rotation;
		miliraPawnFlyer_Hammer.flightDistance = pawn.Position.DistanceTo(destCell);
		miliraPawnFlyer_Hammer.destCell = destCell;
		miliraPawnFlyer_Hammer.pawnWasDrafted = pawn.Drafted;
		miliraPawnFlyer_Hammer.flightEffecterDef = flightEffecterDef;
		miliraPawnFlyer_Hammer.soundLanding = landingSound;
		miliraPawnFlyer_Hammer.triggeringAbility = triggeringAbility?.def;
		miliraPawnFlyer_Hammer.target = target;
		if (pawn.drafter != null)
		{
			miliraPawnFlyer_Hammer.pawnCanFireAtWill = pawn.drafter.FireAtWill;
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
		miliraPawnFlyer_Hammer.jobQueue = pawn.jobs.CaptureAndClearJobQueue();
		if (flyWithCarriedThing && pawn.carryTracker.CarriedThing != null && pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Direct, out miliraPawnFlyer_Hammer.carriedThing))
		{
			if (miliraPawnFlyer_Hammer.carriedThing.holdingOwner != null)
			{
				miliraPawnFlyer_Hammer.carriedThing.holdingOwner.Remove(miliraPawnFlyer_Hammer.carriedThing);
			}
			miliraPawnFlyer_Hammer.carriedThing.DeSpawn();
		}
		if (pawn.Spawned)
		{
			pawn.DeSpawn(DestroyMode.WillReplace);
		}
		if (!miliraPawnFlyer_Hammer.innerContainer.TryAdd(pawn))
		{
			Log.Error("Could not add " + pawn.ToStringSafe() + " to a flyer.");
			pawn.Destroy();
		}
		if (miliraPawnFlyer_Hammer.carriedThing != null && !miliraPawnFlyer_Hammer.innerContainer.TryAdd(miliraPawnFlyer_Hammer.carriedThing))
		{
			Log.Error("Could not add " + miliraPawnFlyer_Hammer.carriedThing.ToStringSafe() + " to a flyer.");
		}
		return miliraPawnFlyer_Hammer;
	}
}
