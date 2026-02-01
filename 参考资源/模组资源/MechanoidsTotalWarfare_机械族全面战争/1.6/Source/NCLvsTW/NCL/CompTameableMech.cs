using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace NCL;

public class CompTameableMech : ThingComp
{
	public CompProperties_TameableMech Props => (CompProperties_TameableMech)props;

	public bool CanBeTamed => parent is Pawn { Faction: null } mech && mech.RaceProps.IsMechanoid && mech.Spawned;

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		foreach (Gizmo item in base.CompGetGizmosExtra())
		{
			yield return item;
		}
		if (CanBeTamed)
		{
			Command_Action tameCommand = new Command_Action
			{
				icon = ContentFinder<Texture2D>.Get("ModIcon/TameMech"),
				defaultLabel = "TameMech".Translate(),
				defaultDesc = "TameMechDesc".Translate(Props.componentsPerTame),
				action = StartTamingProcess,
				hotKey = KeyBindingDefOf.Misc1
			};
			if (!HasEnoughComponentsInMap())
			{
				tameCommand.Disable("NotEnoughComponents".Translate(Props.componentsPerTame));
			}
			else if (FindBestTamer(parent as Pawn) == null)
			{
				tameCommand.Disable("NoAvailableTamer".Translate());
			}
			yield return tameCommand;
		}
	}

	private void StartTamingProcess()
	{
		if (parent is Pawn mech)
		{
			Pawn tamer = FindBestTamer(mech);
			if (tamer != null)
			{
				AddTameDesignation(mech);
				CreateTameJob(tamer, mech);
			}
		}
	}

	private void CreateTameJob(Pawn tamer, Pawn mech)
	{
		Job job = JobMaker.MakeJob(TameMechefOf.TW_TameMech, mech);
		job.count = Props.componentsPerTame;
		job.targetB = FindClosestComponents(tamer);
		tamer.jobs.TryTakeOrderedJob(job, JobTag.Misc);
	}

	private bool HasEnoughComponentsInMap()
	{
		return parent.Map.listerThings.ThingsOfDef(ThingDefOf.ComponentIndustrial).Sum((Thing t) => t.stackCount) >= Props.componentsPerTame;
	}

	private Pawn FindBestTamer(Pawn mech)
	{
		return (from p in mech.Map.mapPawns.FreeColonists
			where !p.WorkTypeIsDisabled(WorkTypeDefOf.Handling)
			orderby p.Position.DistanceTo(mech.Position)
			select p).FirstOrDefault();
	}

	private Thing FindClosestComponents(Pawn pawn)
	{
		return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(ThingDefOf.ComponentIndustrial), PathEndMode.ClosestTouch, TraverseParms.For(pawn), 50f, (Thing t) => !t.IsForbidden(pawn) && pawn.CanReserve(t));
	}

	private void AddTameDesignation(Pawn mech)
	{
		if (mech.Map.designationManager.DesignationOn(mech, TameMechefOf.TW_TameMechDesignation) == null)
		{
			mech.Map.designationManager.AddDesignation(new Designation(mech, TameMechefOf.TW_TameMechDesignation));
		}
	}

	public static void OnTameSuccess(Pawn mech)
	{
		mech.SetFaction(Faction.OfPlayer);
		mech.Map.designationManager.RemoveAllDesignationsOn(mech);
		Messages.Message("MessageMechTamed".Translate(mech.LabelShort), mech, MessageTypeDefOf.PositiveEvent);
	}
}
