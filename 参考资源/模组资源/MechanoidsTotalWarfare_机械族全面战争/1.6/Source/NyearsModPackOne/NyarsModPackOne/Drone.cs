using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace NyarsModPackOne;

public class Drone : Pawn
{
	private const int DrawPosCycleTicks = 60;

	public Pawn owner;

	public int spawnTick;

	public bool activeExplosion;

	public static ModExtension_DroneProperties _modExtension;

	private const float ExplosionRadius = 7.9f;

	private static SimpleCurve drawPosCurve = new SimpleCurve
	{
		new CurvePoint(0f, 0f),
		new CurvePoint(0.6f, 0.3f),
		new CurvePoint(1f, 0f)
	};

	public ModExtension_DroneProperties Props => _modExtension ?? (_modExtension = def.GetModExtension<ModExtension_DroneProperties>());

	public override Vector3 DrawPos => base.DrawPos + new Vector3(0f, 0f, drawPosCurve.Evaluate((float)((Find.TickManager.TicksGame + thingIDNumber) % 60) / 60f));

	protected override void Tick()
	{
		base.Tick();
		if (activeExplosion)
		{
			IntVec3 positionHeld = base.PositionHeld;
			Map mapHeld = base.MapHeld;
			SelfDestroy();
			GenExplosion.DoExplosion(positionHeld, mapHeld, 7.9f, DamageDefOf.Bomb, null, 50);
		}
		else
		{
			int num = Find.TickManager.TicksGame - spawnTick;
			if (num > Props.maxSpawnTime)
			{
				SelfDestroy();
			}
		}
	}

	public override IEnumerable<Gizmo> GetGizmos()
	{
		foreach (Gizmo gizmo in base.GetGizmos())
		{
			yield return gizmo;
		}
		if (DebugSettings.ShowDevGizmos || base.Faction == Faction.OfPlayer)
		{
			Command_Action action = new Command_Action
			{
				defaultLabel = "Boom!",
				defaultDesc = "Boom!",
				icon = ContentFinder<Texture2D>.Get("UI/Commands/Detonate"),
				action = SelfDetonate
			};
			if (base.Downed || !health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
			{
				action.Disable("Incapacitated".Translate());
			}
			yield return action;
			yield return new Command_Action
			{
				defaultLabel = "Destroy",
				defaultDesc = "Destroy",
				icon = ContentFinder<Texture2D>.Get("UI/Commands/TryReconnect"),
				action = SelfDestroy
			};
		}
	}

	private void SelfDetonate()
	{
		Thing thing = (Thing)AttackTargetFinder.BestAttackTarget(this, TargetScanFlags.NeedReachableIfCantHitFromMyPos | TargetScanFlags.NeedThreat | TargetScanFlags.NeedAutoTargetable, (Thing x) => x.Faction != base.Faction, 0f, 9999f, base.PositionHeld, 999999f);
		if (thing == null)
		{
			Messages.Message("No enemy", MessageTypeDefOf.RejectInput, historical: false);
		}
		else
		{
			jobs.StartJob(JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("NCL_Dinergate_DroneDetonate"), thing), JobCondition.Ongoing);
		}
	}

	private void SelfDestroy()
	{
		SetFaction(null);
		Kill(null, null);
		if (base.Corpse != null && !base.Corpse.Destroyed)
		{
			base.Corpse.Destroy();
		}
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_References.Look(ref owner, "owner");
		Scribe_Values.Look(ref spawnTick, "spawnTick", 0);
		Scribe_Values.Look(ref activeExplosion, "activeExplosion", defaultValue: false);
	}

	public static Drone MakeNewDrone(Pawn origin, PawnKindDef droneKind)
	{
		PawnGenerationRequest request = new PawnGenerationRequest(droneKind, origin.Faction, PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: true, allowDead: false, allowDowned: false, canGeneratePawnRelations: false, mustBeCapableOfViolence: true, 1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: false, allowFood: true, allowAddictions: true, inhabitant: false, certainlyBeenInCryptosleep: false, forceRedressWorldPawnIfFormerColonist: false, worldPawnFactionDoesntMatter: false, 0f, 0f, null, 1f, null, null, null, null, 0f, origin.ageTracker.AgeBiologicalYearsFloat, origin.ageTracker.AgeChronologicalYearsFloat);
		Pawn pawn = PawnGenerator.GeneratePawn(request);
		if (!(pawn is Drone drone))
		{
			Log.Error("生成无人机失败，生成的Pawn不是Drone类型。生成的是: " + pawn.GetType().Name);
			return null;
		}
		drone.owner = origin;
		drone.spawnTick = Find.TickManager.TicksGame;
		return drone;
	}
}
