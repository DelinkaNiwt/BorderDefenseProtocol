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
public class MiliraPawnFlyer_Blade : MiliraPawnFlyer
{
	public static float knockBackDistance;

	private static readonly Func<float, float> FlightSpeed_I;

	private static readonly Func<float, float> FlightSpeed_II;

	private static readonly Func<float, float> FlightSpeed_III;

	private static readonly Func<float, float> FlightCurveHeight_I;

	private static readonly Func<float, float> FlightCurveHeight_II;

	private static readonly Func<float, float> FlightCurveHeight_III;

	private static readonly Func<float, float> FlightCurveBladeAngle_I;

	private static readonly Func<float, float> FlightCurveBladeAngle_II;

	private static readonly Func<float, float> FlightCurveBladeAngle_III;

	private static readonly Material bladeEffect;

	private static readonly Material bladeEffect_Slash;

	private bool effecterStarted;

	private int slashNum = 0;

	public Effecter slashEffecter;

	public override bool drawEquipment => false;

	private ThingWithComps weapon => base.FlyingPawn.equipment.Primary;

	private QualityCategory quality => weapon.TryGetComp<CompQuality>().Quality;

	private float damageAmountBase => weapon.def.tools.First().power;

	private float armorPenetrationBase => weapon.def.tools.First().armorPenetration;

	private float damageAmount => 0.2f * AncotUtility.QualityFactor(quality) * damageAmountBase;

	private float armorPenetration => 0.8f * AncotUtility.QualityFactor(quality) * armorPenetrationBase;

	private Ability Ability => base.FlyingPawn.abilities.GetAbility(MiliraDefOf.Milira_Skill_Blade, includeTemporary: true);

	static MiliraPawnFlyer_Blade()
	{
		knockBackDistance = 12f;
		bladeEffect = MaterialPool.MatFrom("Milira/Effect/Blade_Effect", ShaderDatabase.MoteGlow, new Color(1f, 1f, 1f, 1f));
		bladeEffect_Slash = MaterialPool.MatFrom("Milira/Effect/Blade_Effect_Slash", ShaderDatabase.MoteGlow, new Color(1f, 1f, 1f, 1f));
		AnimationCurve animationCurve = new AnimationCurve();
		animationCurve.AddKey(0f, 0f);
		animationCurve.AddKey(0.1f, 0f);
		animationCurve.AddKey(0.2f, 0f);
		animationCurve.AddKey(0.3f, 0f);
		animationCurve.AddKey(0.4f, 0f);
		animationCurve.AddKey(0.5f, 0.03f);
		animationCurve.AddKey(0.6f, 0.1f);
		animationCurve.AddKey(0.7f, 0.3f);
		animationCurve.AddKey(0.8f, 0.6f);
		animationCurve.AddKey(0.9f, 0.9f);
		animationCurve.AddKey(1f, 1f);
		FlightSpeed_I = animationCurve.Evaluate;
		AnimationCurve animationCurve2 = new AnimationCurve();
		animationCurve2.AddKey(0f, 0f);
		animationCurve2.AddKey(0.1f, 0.4f);
		animationCurve2.AddKey(0.2f, 0.6f);
		animationCurve2.AddKey(0.3f, 0.6f);
		animationCurve2.AddKey(0.4f, 0.6f);
		animationCurve2.AddKey(0.5f, 0.65f);
		animationCurve2.AddKey(0.6f, 0.6f);
		animationCurve2.AddKey(0.7f, 0.4f);
		animationCurve2.AddKey(0.8f, 0f);
		animationCurve2.AddKey(0.89f, 0f);
		animationCurve2.AddKey(0.9f, 0.1f);
		animationCurve2.AddKey(0.98f, 0f);
		animationCurve2.AddKey(1f, 0f);
		FlightCurveHeight_I = animationCurve2.Evaluate;
		AnimationCurve animationCurve3 = new AnimationCurve();
		animationCurve3.AddKey(0f, 90f);
		animationCurve3.AddKey(0.1f, 0f);
		animationCurve3.AddKey(0.15f, -80f);
		animationCurve3.AddKey(0.2f, -104f);
		animationCurve3.AddKey(0.3f, -105f);
		animationCurve3.AddKey(0.4f, -105f);
		animationCurve3.AddKey(0.5f, -105f);
		animationCurve3.AddKey(0.6f, -105f);
		animationCurve3.AddKey(0.7f, -104f);
		animationCurve3.AddKey(0.8f, -100f);
		animationCurve3.AddKey(0.9f, 90f);
		animationCurve3.AddKey(1f, 255f);
		FlightCurveBladeAngle_I = animationCurve3.Evaluate;
		AnimationCurve animationCurve4 = new AnimationCurve();
		animationCurve4.AddKey(0f, 0f);
		animationCurve4.AddKey(0.1f, 0f);
		animationCurve4.AddKey(0.3f, 0.01f);
		animationCurve4.AddKey(0.6f, 0.1f);
		animationCurve4.AddKey(0.8f, 0.7f);
		animationCurve4.AddKey(0.9f, 0.9f);
		animationCurve4.AddKey(1f, 1f);
		FlightSpeed_II = animationCurve4.Evaluate;
		AnimationCurve animationCurve5 = new AnimationCurve();
		animationCurve5.AddKey(0f, 0f);
		animationCurve5.AddKey(0.1f, 0.1f);
		animationCurve5.AddKey(0.3f, 0.18f);
		animationCurve5.AddKey(0.6f, 0.2f);
		animationCurve5.AddKey(1f, 0f);
		FlightCurveHeight_II = animationCurve5.Evaluate;
		AnimationCurve animationCurve6 = new AnimationCurve();
		animationCurve6.AddKey(0f, 250f);
		animationCurve6.AddKey(0.1f, 240f);
		animationCurve6.AddKey(0.3f, 240f);
		animationCurve6.AddKey(0.6f, 275f);
		animationCurve6.AddKey(0.657f, 90f);
		animationCurve6.AddKey(0.714f, 225f);
		animationCurve6.AddKey(0.771f, 360f);
		animationCurve6.AddKey(0.828f, 495f);
		animationCurve6.AddKey(0.885f, 630f);
		animationCurve6.AddKey(1f, 765f);
		FlightCurveBladeAngle_II = animationCurve6.Evaluate;
		AnimationCurve animationCurve7 = new AnimationCurve();
		animationCurve7.AddKey(0f, 0f);
		animationCurve7.AddKey(0.1f, 0f);
		animationCurve7.AddKey(0.3f, 0.01f);
		animationCurve7.AddKey(0.6f, 0.1f);
		animationCurve7.AddKey(0.8f, 0.7f);
		animationCurve7.AddKey(0.9f, 0.9f);
		animationCurve7.AddKey(1f, 1f);
		FlightSpeed_III = animationCurve7.Evaluate;
		AnimationCurve animationCurve8 = new AnimationCurve();
		animationCurve8.AddKey(0f, 0f);
		animationCurve8.AddKey(0.2f, 0.3f);
		animationCurve8.AddKey(0.4f, 0.45f);
		animationCurve8.AddKey(0.6f, 0.5f);
		animationCurve8.AddKey(1f, 0f);
		FlightCurveHeight_III = animationCurve8.Evaluate;
		AnimationCurve animationCurve9 = new AnimationCurve();
		animationCurve9.AddKey(0f, -100f);
		animationCurve9.AddKey(0.2f, -110f);
		animationCurve9.AddKey(0.4f, -115f);
		animationCurve9.AddKey(0.6f, -45f);
		animationCurve9.AddKey(0.657f, 90f);
		animationCurve9.AddKey(0.714f, 225f);
		animationCurve9.AddKey(0.771f, 360f);
		animationCurve9.AddKey(0.828f, 495f);
		animationCurve9.AddKey(0.885f, 630f);
		animationCurve9.AddKey(1f, 765f);
		FlightCurveBladeAngle_III = animationCurve9.Evaluate;
	}

	public void SkillPhase(out Func<float, float> speed, out Func<float, float> height, out Func<float, float> blade)
	{
		switch (Ability.RemainingCharges)
		{
		case 2:
			speed = FlightSpeed_I;
			height = FlightCurveHeight_I;
			blade = FlightCurveBladeAngle_I;
			break;
		case 1:
			speed = FlightSpeed_II;
			height = FlightCurveHeight_II;
			blade = FlightCurveBladeAngle_II;
			break;
		case 0:
			speed = FlightSpeed_III;
			height = FlightCurveHeight_III;
			blade = FlightCurveBladeAngle_III;
			break;
		default:
			speed = FlightSpeed_I;
			height = FlightCurveHeight_I;
			blade = FlightCurveBladeAngle_I;
			break;
		}
	}

	protected override void Tick()
	{
		SlashTick();
		base.Tick();
	}

	public void SlashTick()
	{
		if (base.FlyingPawn != null && (float)ticksFlying / (float)ticksFlightTime > SkillPhase())
		{
			if (!effecterStarted)
			{
				slashEffecter = MiliraDefOf.Milira_BladeSlashGhost.Spawn();
				slashEffecter.Trigger(this, TargetInfo.Invalid);
				effecterStarted = true;
			}
			if (slashEffecter != null)
			{
				slashEffecter.EffectTick(this, TargetInfo.Invalid);
			}
			if (base.FlyingPawn.IsHashIntervalTick(2))
			{
				SlashDamage(4f);
			}
		}
	}

	public override void RecomputePosition()
	{
		if (positionLastComputedTick != ticksFlying)
		{
			SkillPhase(out var speed, out var height, out var _);
			positionLastComputedTick = ticksFlying;
			float arg = (float)ticksFlying / (float)ticksFlightTime;
			float t = speed(arg);
			effectiveHeight = 2f * height(arg);
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
		SkillPhase(out var _, out var _, out var blade);
		float arg = (float)ticksFlying / (float)ticksFlightTime;
		float num = base.direction.AngleFlat();
		float num2 = (effectivePos - base.DestinationPos).AngleFlat();
		if (num > 0f && num <= 180f)
		{
			BladeEffect(drawLoc, blade(arg), arg);
		}
		else
		{
			BladeEffect(drawLoc, 0f - blade(arg), arg);
		}
	}

	public virtual void BladeEffect(Vector3 drawLoc, float aimAngle, float arg)
	{
		Mesh mesh = null;
		float num = aimAngle;
		mesh = ((aimAngle > 20f && aimAngle < 160f) ? MeshPool.plane10 : ((!(aimAngle > 200f) || !(aimAngle < 340f)) ? MeshPool.plane10 : MeshPool.plane10Flip));
		num %= 360f;
		Material material = null;
		material = BladeEffect(arg);
		drawLoc.y = AltitudeLayer.PawnUnused.AltitudeFor();
		Matrix4x4 matrix = Matrix4x4.TRS(s: new Vector3(4f, 0f, 8f), pos: drawLoc, q: Quaternion.AngleAxis(num, Vector3.up));
		Graphics.DrawMesh(mesh, matrix, material, 0);
	}

	public Material BladeEffect(float arg)
	{
		if (arg < SkillPhase())
		{
			return bladeEffect;
		}
		return bladeEffect_Slash;
	}

	public float SkillPhase()
	{
		return Ability.RemainingCharges switch
		{
			2 => 0.8f, 
			1 => 0.6f, 
			0 => 0.6f, 
			_ => 0.8f, 
		};
	}

	public override void LandingEffects()
	{
		effecterStarted = false;
		slashNum = 0;
		if (slashEffecter != null)
		{
			slashEffecter.Cleanup();
		}
		base.LandingEffects();
		if (Ability.RemainingCharges == 3)
		{
			SlashDamage(6f);
			ForceMovementUtility.ApplyRepulsiveForceArea(base.Position, base.FlyingPawn, base.Map, 4f, 3f, (List<HediffDef>)null, true, 1f, false);
			Effecter effecter = MiliraDefOf.Milira_BladeSlashGhostEndUp.Spawn();
			effecter.Trigger(this, TargetInfo.Invalid);
			effecter.Cleanup();
			base.FlyingPawn.stances.SetStance(new Stance_Cooldown(60, null, null));
		}
	}

	private void SlashDamage(float radius)
	{
		IntVec3 center = DrawPos.ToIntVec3();
		foreach (Thing item in GenRadial.RadialDistinctThingsAround(center, base.Map, radius, useCenter: true))
		{
			if (item is Pawn pawn && pawn.Faction != base.FlyingPawn.Faction && !pawn.Downed)
			{
				AncotUtility.DoDamage((Thing)pawn, DamageDefOf.Stab, damageAmount, armorPenetration, (Thing)base.FlyingPawn);
				slashNum++;
			}
		}
	}

	public new static MiliraPawnFlyer_Blade MakeFlyer(ThingDef flyingDef, Pawn pawn, IntVec3 destCell, EffecterDef flightEffecterDef, SoundDef landingSound, bool flyWithCarriedThing = false, Vector3? overrideStartVec = null, Ability triggeringAbility = null, LocalTargetInfo target = default(LocalTargetInfo))
	{
		MiliraPawnFlyer_Blade miliraPawnFlyer_Blade = (MiliraPawnFlyer_Blade)ThingMaker.MakeThing(flyingDef);
		miliraPawnFlyer_Blade.startVec = overrideStartVec ?? pawn.TrueCenter();
		miliraPawnFlyer_Blade.Rotation = pawn.Rotation;
		miliraPawnFlyer_Blade.flightDistance = pawn.Position.DistanceTo(destCell);
		miliraPawnFlyer_Blade.destCell = destCell;
		miliraPawnFlyer_Blade.pawnWasDrafted = pawn.Drafted;
		miliraPawnFlyer_Blade.flightEffecterDef = flightEffecterDef;
		miliraPawnFlyer_Blade.soundLanding = landingSound;
		miliraPawnFlyer_Blade.triggeringAbility = triggeringAbility?.def;
		miliraPawnFlyer_Blade.target = target;
		if (pawn.drafter != null)
		{
			miliraPawnFlyer_Blade.pawnCanFireAtWill = pawn.drafter.FireAtWill;
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
		miliraPawnFlyer_Blade.jobQueue = pawn.jobs.CaptureAndClearJobQueue();
		if (flyWithCarriedThing && pawn.carryTracker.CarriedThing != null && pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Direct, out miliraPawnFlyer_Blade.carriedThing))
		{
			if (miliraPawnFlyer_Blade.carriedThing.holdingOwner != null)
			{
				miliraPawnFlyer_Blade.carriedThing.holdingOwner.Remove(miliraPawnFlyer_Blade.carriedThing);
			}
			miliraPawnFlyer_Blade.carriedThing.DeSpawn();
		}
		if (pawn.Spawned)
		{
			pawn.DeSpawn(DestroyMode.WillReplace);
		}
		if (!miliraPawnFlyer_Blade.innerContainer.TryAdd(pawn))
		{
			Log.Error("Could not add " + pawn.ToStringSafe() + " to a flyer.");
			pawn.Destroy();
		}
		if (miliraPawnFlyer_Blade.carriedThing != null && !miliraPawnFlyer_Blade.innerContainer.TryAdd(miliraPawnFlyer_Blade.carriedThing))
		{
			Log.Error("Could not add " + miliraPawnFlyer_Blade.carriedThing.ToStringSafe() + " to a flyer.");
		}
		return miliraPawnFlyer_Blade;
	}
}
