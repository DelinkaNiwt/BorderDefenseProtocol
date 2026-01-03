using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace AncotLibrary;

public class PawnJumper_Custom : Thing, IThingHolderTickable, IThingHolder, ISearchableContents
{
	protected ThingOwner<Thing> innerContainer;

	protected Vector3 startVec;

	protected IntVec3 destCell;

	protected float flightDistance;

	protected bool pawnWasDrafted;

	protected bool pawnCanFireAtWill = true;

	protected int ticksFlightTime = 120;

	protected int ticksFlying;

	protected JobQueue jobQueue;

	protected EffecterDef flightEffecterDef;

	protected SoundDef soundLanding;

	protected Thing carriedThing;

	protected LocalTargetInfo target;

	protected AbilityDef triggeringAbility;

	protected Effecter flightEffecter;

	protected int positionLastComputedTick = -1;

	protected Vector3 groundPos;

	protected Vector3 effectivePos;

	protected float effectiveHeight;

	private const int CheckDestinationInterval = 15;

	public bool ShouldTickContents => ticksFlying < ticksFlightTime;

	protected Thing FlyingThing
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

	public ThingOwner SearchableContents => innerContainer;

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
			groundPos = Vector3.Lerp(startVec, DestinationPos, t2);
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

	public PawnJumper_Custom()
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
		if (!respawningAfterLoad && !base.BeingTransportedOnGravship)
		{
			float a = Mathf.Max(flightDistance, 1f) / def.pawnFlyer.flightSpeed;
			a = Mathf.Max(a, def.pawnFlyer.flightDurationMin);
			ticksFlightTime = a.SecondsToTicks();
			ticksFlying = 0;
		}
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

	public virtual void RespawnPawn()
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
			(comp as ICompAbilityEffectOnJumpCompleted)?.OnJumpCompleted(startVec.ToIntVec3(), target);
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

	private void CheckDestination()
	{
		if (JumpUtility_Custom.ValidJumpTarget(FlyingThing, base.Map, destCell))
		{
			return;
		}
		int num = GenRadial.NumCellsInRadius(3.9f);
		for (int i = 0; i < num; i++)
		{
			IntVec3 cell = destCell + GenRadial.RadialPattern[i];
			if (JumpUtility_Custom.ValidJumpTarget(FlyingThing, base.Map, cell))
			{
				destCell = cell;
				break;
			}
		}
	}

	public void Notify_TransportedOnGravship(Gravship gravship)
	{
		IntVec3 intVec = gravship.Engine.Position - gravship.originalPosition;
		startVec += intVec.ToVector3();
		destCell += intVec;
		positionLastComputedTick = -1;
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

	public static PawnJumper_Custom MakeFlyer(ThingDef flyingDef, Pawn pawn, IntVec3 destCell, EffecterDef flightEffecterDef, SoundDef landingSound, bool flyWithCarriedThing = false, Vector3? overrideStartVec = null, Ability triggeringAbility = null, LocalTargetInfo target = default(LocalTargetInfo))
	{
		PawnJumper_Custom pawnJumper_Custom = (PawnJumper_Custom)ThingMaker.MakeThing(flyingDef);
		pawnJumper_Custom.startVec = overrideStartVec ?? pawn.TrueCenter();
		pawnJumper_Custom.Rotation = pawn.Rotation;
		pawnJumper_Custom.flightDistance = pawn.Position.DistanceTo(destCell);
		pawnJumper_Custom.destCell = destCell;
		pawnJumper_Custom.pawnWasDrafted = pawn.Drafted;
		pawnJumper_Custom.flightEffecterDef = flightEffecterDef;
		pawnJumper_Custom.soundLanding = landingSound;
		pawnJumper_Custom.triggeringAbility = triggeringAbility?.def;
		pawnJumper_Custom.target = target;
		if (pawn.drafter != null)
		{
			pawnJumper_Custom.pawnCanFireAtWill = pawn.drafter.FireAtWill;
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
		pawnJumper_Custom.jobQueue = pawn.jobs.CaptureAndClearJobQueue();
		if (flyWithCarriedThing && pawn.carryTracker.CarriedThing != null && pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Direct, out pawnJumper_Custom.carriedThing))
		{
			if (pawnJumper_Custom.carriedThing.holdingOwner != null)
			{
				pawnJumper_Custom.carriedThing.holdingOwner.Remove(pawnJumper_Custom.carriedThing);
			}
			pawnJumper_Custom.carriedThing.DeSpawn();
		}
		if (pawn.Spawned)
		{
			pawn.DeSpawn(DestroyMode.WillReplace);
		}
		if (!pawnJumper_Custom.innerContainer.TryAdd(pawn))
		{
			Log.Error("Could not add " + pawn.ToStringSafe() + " to a flyer.");
			pawn.Destroy();
		}
		if (pawnJumper_Custom.carriedThing != null && !pawnJumper_Custom.innerContainer.TryAdd(pawnJumper_Custom.carriedThing))
		{
			Log.Error("Could not add " + pawnJumper_Custom.carriedThing.ToStringSafe() + " to a flyer.");
		}
		return pawnJumper_Custom;
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
