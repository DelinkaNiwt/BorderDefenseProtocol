using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace Milira;

public class MiliraPawnFlyer : Thing, IThingHolder
{
	public ThingOwner<Thing> innerContainer;

	public Vector3 startVec;

	public IntVec3 destCell;

	public float flightDistance;

	public bool pawnWasDrafted;

	public bool pawnCanFireAtWill = true;

	public int ticksFlightTime = 120;

	public int ticksFlying;

	public JobQueue jobQueue;

	protected EffecterDef flightEffecterDef;

	protected SoundDef soundLanding;

	public Thing carriedThing;

	public LocalTargetInfo target;

	public AbilityDef triggeringAbility;

	public Effecter flightEffecter;

	public int positionLastComputedTick = -1;

	public Vector3 groundPos;

	public Vector3 effectivePos;

	public float effectiveHeight;

	public virtual bool drawEquipment => true;

	public Vector3 direction => (DestinationPos - startVec).normalized;

	public Thing FlyingThing
	{
		get
		{
			if (innerContainer.InnerListForReading.Count <= 0)
			{
				return null;
			}
			return innerContainer.InnerListForReading[0];
		}
	}

	public Pawn FlyingPawn => FlyingThing as Pawn;

	public Thing CarriedThing => carriedThing;

	public override Vector3 DrawPos
	{
		get
		{
			RecomputePosition();
			return effectivePos;
		}
	}

	public Vector3 DestinationPos
	{
		get
		{
			Thing flyingThing = FlyingThing;
			return GenThing.TrueCenter(destCell, flyingThing.Rotation, flyingThing.def.size, flyingThing.def.Altitude);
		}
	}

	public virtual void RecomputePosition()
	{
		if (positionLastComputedTick != ticksFlying)
		{
			positionLastComputedTick = ticksFlying;
			float t = (float)ticksFlying / (float)ticksFlightTime;
			float t2 = def.pawnFlyer.Worker.AdjustedProgress(t);
			effectiveHeight = def.pawnFlyer.Worker.GetHeight(t2);
			groundPos = Vector3.LerpUnclamped(startVec, DestinationPos, t2);
			Vector3 vector = Altitudes.AltIncVect * effectiveHeight;
			Vector3 vector2 = Vector3.forward * (def.pawnFlyer.heightFactor * effectiveHeight);
			effectivePos = groundPos + vector + vector2;
			base.Position = groundPos.ToIntVec3();
		}
	}

	public ThingOwner GetDirectlyHeldThings()
	{
		return innerContainer;
	}

	public MiliraPawnFlyer()
	{
		innerContainer = new ThingOwner<Thing>(this);
	}

	public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
	{
		flightEffecter?.Cleanup();
		base.Destroy(mode);
	}

	public void GetChildHolders(List<IThingHolder> outChildren)
	{
		ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
	}

	public override void SpawnSetup(Map map, bool respawningAfterLoad)
	{
		base.SpawnSetup(map, respawningAfterLoad);
		if (!respawningAfterLoad)
		{
			float a = Mathf.Max(flightDistance, 1f) / def.pawnFlyer.flightSpeed;
			a = Mathf.Max(a, def.pawnFlyer.flightDurationMin);
			ticksFlightTime = a.SecondsToTicks();
			ticksFlying = 0;
		}
	}

	protected virtual void RespawnPawn()
	{
		Thing flyingThing = FlyingThing;
		LandingEffects();
		innerContainer.TryDrop(flyingThing, destCell, flyingThing.MapHeld, ThingPlaceMode.Direct, out var lastResultingThing, null, null, playDropSound: false);
		Pawn pawn = flyingThing as Pawn;
		if (pawn?.drafter != null)
		{
			pawn.drafter.Drafted = pawnWasDrafted;
			pawn.drafter.FireAtWill = pawnCanFireAtWill;
		}
		flyingThing.Rotation = base.Rotation;
		if (carriedThing != null && innerContainer.TryDrop(carriedThing, destCell, flyingThing.MapHeld, ThingPlaceMode.Direct, out lastResultingThing, null, null, playDropSound: false) && pawn != null)
		{
			carriedThing.DeSpawn();
			if (!pawn.carryTracker.TryStartCarry(carriedThing))
			{
				Log.Error("Could not carry " + carriedThing.ToStringSafe() + " after respawning flyer pawn.");
			}
		}
		if (pawn == null)
		{
			return;
		}
		if (jobQueue != null)
		{
			pawn.jobs.RestoreCapturedJobs(jobQueue);
		}
		pawn.jobs.CheckForJobOverride();
		if (def.pawnFlyer.stunDurationTicksRange != IntRange.Zero)
		{
			pawn.stances.stunner.StunFor(def.pawnFlyer.stunDurationTicksRange.RandomInRange, null, addBattleLog: false, showMote: false);
		}
		if (triggeringAbility == null)
		{
			return;
		}
		Ability ability = pawn.abilities.GetAbility(triggeringAbility);
		if (ability?.comps == null)
		{
			return;
		}
		foreach (AbilityComp comp in ability.comps)
		{
			if (comp is ICompAbilityEffectOnJumpCompleted compAbilityEffectOnJumpCompleted)
			{
				compAbilityEffectOnJumpCompleted.OnJumpCompleted(startVec.ToIntVec3(), target);
			}
		}
		if (pawn.Drawer.renderer.CurAnimation != null)
		{
			pawn.Drawer.renderer.SetAnimation(null);
		}
	}

	public virtual void LandingEffects()
	{
		soundLanding?.PlayOneShot(new TargetInfo(base.Position, base.Map));
		FleckMaker.ThrowDustPuff(DestinationPos + Gen.RandomHorizontalVector(0.5f), base.Map, 2f);
	}

	protected override void TickInterval(int delta)
	{
	}

	protected override void Tick()
	{
		if (flightEffecter == null && flightEffecterDef != null)
		{
			flightEffecter = flightEffecterDef.Spawn();
			flightEffecter.Trigger(this, target.ToTargetInfo(base.Map));
		}
		else
		{
			flightEffecter?.EffectTick(this, target.ToTargetInfo(base.Map));
		}
		if (ticksFlying >= ticksFlightTime)
		{
			RespawnPawn();
			Destroy();
		}
		else
		{
			if (ticksFlying % 5 == 0)
			{
				CheckDestination();
			}
			innerContainer.DoTick();
		}
		ticksFlying++;
	}

	public virtual void CheckDestination()
	{
		if (MiliraFlyUtility.ValidJumpTarget(base.Map, destCell))
		{
			return;
		}
		int num = GenRadial.NumCellsInRadius(3.9f);
		for (int i = 0; i < num; i++)
		{
			IntVec3 cell = destCell + GenRadial.RadialPattern[i];
			if (MiliraFlyUtility.ValidJumpTarget(base.Map, cell))
			{
				destCell = cell;
				break;
			}
		}
	}

	public override void DynamicDrawPhaseAt(DrawPhase phase, Vector3 drawLoc, bool flip = false)
	{
		RecomputePosition();
		if (FlyingPawn != null)
		{
			FlyingPawn.DynamicDrawPhaseAt(phase, effectivePos);
		}
		else
		{
			FlyingThing?.DynamicDrawPhaseAt(phase, effectivePos);
		}
		base.DynamicDrawPhaseAt(phase, drawLoc, flip);
	}

	protected override void DrawAt(Vector3 drawLoc, bool flip = false)
	{
		DrawShadow(groundPos, effectiveHeight);
		if (CarriedThing != null && FlyingPawn != null)
		{
			PawnRenderUtility.DrawCarriedThing(FlyingPawn, effectivePos, CarriedThing);
		}
		if (drawEquipment)
		{
			float num = direction.AngleFlat();
		}
	}

	private void DrawShadow(Vector3 drawLoc, float height)
	{
		Material shadowMaterial = def.pawnFlyer.ShadowMaterial;
		if (!(shadowMaterial == null))
		{
			float num = Mathf.Lerp(1f, 0.6f, height);
			Vector3 s = new Vector3(num, 1f, num);
			Matrix4x4 matrix = default(Matrix4x4);
			matrix.SetTRS(drawLoc, Quaternion.identity, s);
			Graphics.DrawMesh(MeshPool.plane10, matrix, shadowMaterial, 0);
		}
	}

	public static MiliraPawnFlyer MakeFlyer(ThingDef flyingDef, Pawn pawn, IntVec3 destCell, EffecterDef flightEffecterDef, SoundDef landingSound, bool flyWithCarriedThing = false, Vector3? overrideStartVec = null, Ability triggeringAbility = null, LocalTargetInfo target = default(LocalTargetInfo))
	{
		MiliraPawnFlyer miliraPawnFlyer = (MiliraPawnFlyer)ThingMaker.MakeThing(flyingDef);
		miliraPawnFlyer.startVec = overrideStartVec ?? pawn.TrueCenter();
		miliraPawnFlyer.Rotation = pawn.Rotation;
		miliraPawnFlyer.flightDistance = pawn.Position.DistanceTo(destCell);
		miliraPawnFlyer.destCell = destCell;
		miliraPawnFlyer.pawnWasDrafted = pawn.Drafted;
		miliraPawnFlyer.flightEffecterDef = flightEffecterDef;
		miliraPawnFlyer.soundLanding = landingSound;
		miliraPawnFlyer.triggeringAbility = triggeringAbility?.def;
		miliraPawnFlyer.target = target;
		if (pawn.drafter != null)
		{
			miliraPawnFlyer.pawnCanFireAtWill = pawn.drafter.FireAtWill;
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
		miliraPawnFlyer.jobQueue = pawn.jobs.CaptureAndClearJobQueue();
		if (flyWithCarriedThing && pawn.carryTracker.CarriedThing != null && pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Direct, out miliraPawnFlyer.carriedThing))
		{
			if (miliraPawnFlyer.carriedThing.holdingOwner != null)
			{
				miliraPawnFlyer.carriedThing.holdingOwner.Remove(miliraPawnFlyer.carriedThing);
			}
			miliraPawnFlyer.carriedThing.DeSpawn();
		}
		if (pawn.Spawned)
		{
			pawn.DeSpawn(DestroyMode.WillReplace);
		}
		if (!miliraPawnFlyer.innerContainer.TryAdd(pawn))
		{
			Log.Error("Could not add " + pawn.ToStringSafe() + " to a flyer.");
			pawn.Destroy();
		}
		if (miliraPawnFlyer.carriedThing != null && !miliraPawnFlyer.innerContainer.TryAdd(miliraPawnFlyer.carriedThing))
		{
			Log.Error("Could not add " + miliraPawnFlyer.carriedThing.ToStringSafe() + " to a flyer.");
		}
		return miliraPawnFlyer;
	}

	public virtual void DrawEquipmentFlying(Thing eq, Vector3 drawLoc, float aimAngle)
	{
		if (eq == null)
		{
			return;
		}
		Mesh mesh = null;
		float num = aimAngle - 90f;
		if (aimAngle > 20f && aimAngle < 160f)
		{
			mesh = MeshPool.plane10;
			num += eq.def.equippedAngleOffset;
		}
		else if (aimAngle > 200f && aimAngle < 340f)
		{
			mesh = MeshPool.plane10Flip;
			num -= 180f;
			num -= eq.def.equippedAngleOffset;
		}
		else
		{
			mesh = MeshPool.plane10;
			num += eq.def.equippedAngleOffset;
		}
		num %= 360f;
		CompEquippable compEquippable = eq.TryGetComp<CompEquippable>();
		if (compEquippable != null)
		{
			EquipmentUtility.Recoil(eq.def, EquipmentUtility.GetRecoilVerb(compEquippable.AllVerbs), out var drawOffset, out var angleOffset, aimAngle);
			drawLoc += drawOffset;
			num += angleOffset;
			drawLoc.x += 0.6f * direction.x;
			drawLoc.z += 0.6f * direction.z;
			if (aimAngle > 45f && aimAngle < 315f)
			{
				drawLoc.y = AltitudeLayer.Skyfaller.AltitudeFor() + 0.02f;
			}
		}
		Material material = null;
		material = ((!(eq.Graphic is Graphic_StackCount graphic_StackCount)) ? eq.Graphic.MatSingleFor(eq) : graphic_StackCount.SubGraphicForStackCount(1, eq.def).MatSingleFor(eq));
		Matrix4x4 matrix = Matrix4x4.TRS(s: new Vector3(eq.Graphic.drawSize.x, 0f, eq.Graphic.drawSize.y), pos: drawLoc, q: Quaternion.AngleAxis(num, Vector3.up));
		Graphics.DrawMesh(mesh, matrix, material, 0);
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref startVec, "startVec");
		Scribe_Values.Look(ref destCell, "destCell");
		Scribe_Values.Look(ref flightDistance, "flightDistance", 0f);
		Scribe_Values.Look(ref pawnWasDrafted, "pawnWasDrafted", defaultValue: false);
		Scribe_Values.Look(ref pawnCanFireAtWill, "pawnCanFireAtWill", defaultValue: true);
		Scribe_Values.Look(ref ticksFlightTime, "ticksFlightTime", 0);
		Scribe_Values.Look(ref ticksFlying, "ticksFlying", 0);
		Scribe_Defs.Look(ref flightEffecterDef, "flightEffecterDef");
		Scribe_Defs.Look(ref soundLanding, "soundLanding");
		Scribe_Defs.Look(ref triggeringAbility, "triggeringAbility");
		Scribe_References.Look(ref carriedThing, "carriedThing");
		Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
		Scribe_Deep.Look(ref jobQueue, "jobQueue");
		Scribe_TargetInfo.Look(ref target, "target");
	}
}
