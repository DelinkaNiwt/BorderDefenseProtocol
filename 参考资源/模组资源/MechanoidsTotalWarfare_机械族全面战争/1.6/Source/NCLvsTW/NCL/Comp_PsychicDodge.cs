using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace NCL;

public class Comp_PsychicDodge : ThingComp
{
	private int lastDodgeTick = -9999;

	private bool enabled = true;

	private const int DODGE_COOLDOWN = 360;

	private const float TRIGGER_RADIUS = 3f;

	private const float MIN_DODGE_DISTANCE = 8f;

	private const float MAX_DODGE_DISTANCE = 10f;

	private const int CHECK_INTERVAL = 5;

	public CompProperties_PsychicDodge Props => (CompProperties_PsychicDodge)props;

	public override void PostPostMake()
	{
		base.PostPostMake();
		lastDodgeTick = Find.TickManager.TicksGame - 360;
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		if (parent is Pawn pawn && pawn.Faction == Faction.OfPlayer && !pawn.Dead)
		{
			yield return new Command_Toggle
			{
				defaultLabel = "NCL.PsychicDodgeToggle".Translate(),
				defaultDesc = "NCL.PsychicDodgeToggleDesc".Translate(),
				icon = ContentFinder<Texture2D>.Get("UI/Abilities/Skip"),
				isActive = () => enabled,
				toggleAction = delegate
				{
					enabled = !enabled;
				}
			};
		}
	}

	public override void CompTick()
	{
		base.CompTick();
		if (enabled && Find.TickManager.TicksGame % 5 == 0 && parent is Pawn { Downed: false, Dead: false, Map: not null } pawn && Find.TickManager.TicksGame >= lastDodgeTick + 360)
		{
			IntVec3? threatPosition = FindNearbyThreatPosition(pawn);
			if (threatPosition.HasValue)
			{
				TriggerDodge(pawn, threatPosition.Value);
			}
		}
	}

	private IntVec3? FindNearbyThreatPosition(Pawn pawn)
	{
		Map map = pawn.Map;
		IntVec3 pawnPosition = pawn.Position;
		List<Thing> projectiles = map.listerThings.ThingsInGroup(ThingRequestGroup.Projectile);
		foreach (Thing thing in projectiles)
		{
			if (thing is Projectile proj && pawnPosition.DistanceTo(proj.Position) <= 3f && IsHostileProjectile(proj, pawn))
			{
				return proj.Position;
			}
		}
		foreach (Pawn otherPawn in map.mapPawns.AllPawnsSpawned)
		{
			if (otherPawn != pawn && !otherPawn.Downed && !otherPawn.Dead && pawnPosition.DistanceTo(otherPawn.Position) <= 3f && IsHostilePawn(otherPawn, pawn))
			{
				return otherPawn.Position;
			}
		}
		return null;
	}

	private bool IsHostileProjectile(Projectile projectile, Pawn pawn)
	{
		Thing launcher = projectile.Launcher;
		if (launcher?.Faction != null && pawn.Faction != null)
		{
			return pawn.Faction.HostileTo(launcher.Faction);
		}
		if (launcher is Pawn launcherPawn)
		{
			return launcherPawn.HostileTo(pawn);
		}
		return false;
	}

	private bool IsHostilePawn(Pawn otherPawn, Pawn selfPawn)
	{
		if (selfPawn.Faction != null && otherPawn.Faction != null)
		{
			return selfPawn.Faction.HostileTo(otherPawn.Faction);
		}
		return selfPawn.HostileTo(otherPawn);
	}

	private void TriggerDodge(Pawn pawn, IntVec3 threatPosition)
	{
		Vector3 awayDirection = (pawn.Position - threatPosition).ToVector3().normalized;
		float randomAngle = Rand.Range(-90f, 90f);
		Quaternion rotation = Quaternion.AngleAxis(randomAngle, Vector3.up);
		Vector3 dodgeDirection = rotation * awayDirection;
		dodgeDirection.Normalize();
		float dodgeDistance = Rand.Range(8f, 10f);
		IntVec3 targetPos = GetSafePosition(pawn, dodgeDirection, dodgeDistance);
		ExecuteTeleport(pawn, targetPos);
		lastDodgeTick = Find.TickManager.TicksGame;
	}

	private IntVec3 GetSafePosition(Pawn pawn, Vector3 direction, float distance)
	{
		Map map = pawn.Map;
		IntVec3 rawTarget = pawn.Position + (direction * distance).ToIntVec3();
		if (rawTarget.InBounds(map) && rawTarget.Standable(map) && !rawTarget.Impassable(map) && !map.roofGrid.Roofed(rawTarget))
		{
			return rawTarget;
		}
		int searchRadius = Mathf.FloorToInt(distance / 2f);
		IntVec3 safePosition = pawn.Position;
		if (CellFinder.TryFindRandomCellNear(rawTarget, map, searchRadius, (IntVec3 c) => c.Standable(map) && !c.Impassable(map) && c.InBounds(map) && !map.roofGrid.Roofed(c), out var foundPos))
		{
			safePosition = foundPos;
		}
		return safePosition;
	}

	private void ExecuteTeleport(Pawn pawn, IntVec3 targetPos)
	{
		Map map = pawn.Map;
		bool wasDrafted = pawn.Drafted;
		Job curJob = pawn.CurJob;
		MentalState mentalState = pawn.MentalState;
		Thing carriedThing = pawn.carryTracker?.CarriedThing;
		bool hadCarriedThing = carriedThing != null;
		FleckMaker.Static(pawn.Position, map, FleckDefOf.PsycastSkipFlashEntry);
		pawn.Position = targetPos;
		pawn.Notify_Teleported();
		if (wasDrafted)
		{
			pawn.drafter.Drafted = true;
		}
		if (mentalState != null)
		{
			pawn.mindState.mentalStateHandler.TryStartMentalState(mentalState.def, null, forced: false, forceWake: false, causedByMood: false, null, transitionSilently: true);
		}
		if (hadCarriedThing && carriedThing != null && carriedThing.SpawnedOrAnyParentSpawned)
		{
			pawn.carryTracker.TryStartCarry(carriedThing);
		}
		if (curJob != null && pawn.jobs.curDriver == null)
		{
			pawn.jobs.StartJob(curJob, JobCondition.InterruptForced);
		}
		FleckMaker.Static(targetPos, map, FleckDefOf.PsycastSkipInnerExit);
		SoundDefOf.Psycast_Skip_Entry.PlayOneShot(new TargetInfo(pawn.Position, map));
		SoundDefOf.Psycast_Skip_Exit.PlayOneShot(new TargetInfo(targetPos, map));
	}

	public override string CompInspectStringExtra()
	{
		if (!enabled)
		{
			return "NCL.PsychicDodgeDisabled".Translate();
		}
		int ticksRemaining = lastDodgeTick + 360 - Find.TickManager.TicksGame;
		if (ticksRemaining > 0)
		{
			return "NCL.PsychicDodgeCoolingDown".Translate((float)ticksRemaining / 60f);
		}
		return "NCL.PsychicDodgeReady".Translate();
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref enabled, "psychicDodgeEnabled", defaultValue: true);
		Scribe_Values.Look(ref lastDodgeTick, "lastDodgeTick", -9999);
	}
}
