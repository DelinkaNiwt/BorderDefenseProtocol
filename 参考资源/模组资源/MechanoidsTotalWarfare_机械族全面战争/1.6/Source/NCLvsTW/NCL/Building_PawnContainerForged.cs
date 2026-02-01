using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace NCL;

public class Building_PawnContainerForged : Building, IThingHolder
{
	private ThingOwner<Pawn> innerContainer;

	private Hediff sleepingHediff;

	public Pawn ContainedPawn => (innerContainer.Count > 0) ? innerContainer[0] : null;

	public Building_PawnContainerForged()
	{
		innerContainer = new ThingOwner<Pawn>(this);
	}

	public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
	{
		bool released = ReleasePawn();
		base.Destroy(mode);
		if (!released)
		{
			Log.Warning("Pawn release may have failed during building destruction");
		}
	}

	private bool ReleasePawn()
	{
		if (ContainedPawn == null)
		{
			Log.Warning("No contained pawn to release");
			return false;
		}
		Pawn pawn = ContainedPawn;
		Map map = base.Map;
		IntVec3 position = base.Position;
		try
		{
			if (sleepingHediff != null && pawn.health != null)
			{
				pawn.health.RemoveHediff(sleepingHediff);
			}
			if (innerContainer.Contains(pawn))
			{
				innerContainer.Remove(pawn);
			}
			else
			{
				Log.Warning("Pawn was not in container as expected");
			}
			if (map == null || !position.IsValid || (map != null && !position.InBounds(map)))
			{
				Log.Error($"Invalid spawn conditions - Map: {map}, Position: {position}");
				return false;
			}
			if (!pawn.Spawned)
			{
				GenSpawn.Spawn(pawn, position, map, Rot4.Random);
			}
			else
			{
				Log.Warning("Pawn was already spawned when releasing");
			}
			pawn.jobs?.EndCurrentJob(JobCondition.InterruptForced);
			if (map != null)
			{
				FleckMaker.ThrowDustPuff(position, map, 1f);
			}
			return true;
		}
		catch (Exception arg)
		{
			Log.Error($"Exception releasing pawn {pawn}: {arg}");
			return false;
		}
	}

	public override IEnumerable<Gizmo> GetGizmos()
	{
		foreach (Gizmo gizmo in base.GetGizmos())
		{
			yield return gizmo;
		}
		if (ContainedPawn != null && (ContainedPawn.Faction == Faction.OfPlayer || DebugSettings.ShowDevGizmos))
		{
			Command_Action releaseCommand = new Command_Action
			{
				defaultLabel = "Release " + ContainedPawn.LabelShortCap,
				defaultDesc = "Release the contained pawn back into the world.",
				icon = (ContentFinder<Texture2D>.Get("Ability/ReleaseFromBuilding", reportFailure: false) ?? BaseContent.BadTex),
				action = delegate
				{
					Destroy();
				}
			};
			if (ContainedPawn.Downed)
			{
				releaseCommand.Disable("Incapacitated".Translate());
			}
			yield return releaseCommand;
		}
	}

	public void GetChildHolders(List<IThingHolder> outChildren)
	{
		ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
	}

	public ThingOwner GetDirectlyHeldThings()
	{
		return innerContainer;
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
		Scribe_References.Look(ref sleepingHediff, "sleepingHediff");
	}

	public static Building_PawnContainerForged MakeSleepingBuilding(Pawn pawn)
	{
		if (pawn == null)
		{
			Log.Error("Tried to create container building for null pawn");
			return null;
		}
		Map map = pawn.Map;
		IntVec3 position = pawn.Position;
		Faction faction = pawn.Faction;
		if (!(ThingMaker.MakeThing(NCLContainerDefOf.Building_PawnContainerForged) is Building_PawnContainerForged building))
		{
			Log.Error("Failed to create Building_PawnContainer instance");
			return null;
		}
		building.SetFaction(faction);
		if (pawn.Spawned)
		{
			pawn.DeSpawn();
		}
		building.sleepingHediff = pawn.health.AddHediff(NCLContainerDefOf.PawnContainedHediff);
		if (!building.innerContainer.TryAdd(pawn))
		{
			Log.Error("Failed to add " + pawn.Label + " to container");
			return null;
		}
		return building;
	}

	protected override void Tick()
	{
		base.Tick();
		innerContainer.DoTick();
	}
}
