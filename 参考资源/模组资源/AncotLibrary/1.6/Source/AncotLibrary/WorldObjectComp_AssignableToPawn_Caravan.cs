using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace AncotLibrary;

[StaticConstructorOnStartup]
public class WorldObjectComp_AssignableToPawn_Caravan : WorldObjectComp
{
	protected List<Pawn> assignedPawns = new List<Pawn>();

	protected List<Pawn> uninstalledAssignedPawns = new List<Pawn>();

	public WorldObjectCompProperties_AssignableToPawn_Caravan Props => (WorldObjectCompProperties_AssignableToPawn_Caravan)props;

	public int MaxAssignedPawnsCount => Props.maxAssignedPawnsCount;

	public List<Pawn> AssignedPawnsForReading => assignedPawns;

	public IEnumerable<Pawn> AssignedPawns => assignedPawns;

	public bool HasFreeSlot => assignedPawns.Count < Props.maxAssignedPawnsCount;

	public int TotalSlots => Props.maxAssignedPawnsCount;

	public virtual IEnumerable<Pawn> AssigningCandidates(Caravan caravan)
	{
		List<Pawn> list = new List<Pawn>();
		List<Pawn> pawnsListForReading = caravan.PawnsListForReading;
		for (int i = 0; i < pawnsListForReading.Count; i++)
		{
			if (pawnsListForReading[i].kindDef.RaceProps.Humanlike)
			{
				list.Add(pawnsListForReading[i]);
			}
		}
		return list;
	}

	protected virtual void SortAssignedPawns()
	{
		assignedPawns.RemoveAll((Pawn x) => x == null);
		assignedPawns.SortBy((Pawn x) => x.thingIDNumber);
	}

	public virtual void ForceAddPawn(Pawn pawn)
	{
		if (!assignedPawns.Contains(pawn))
		{
			assignedPawns.Add(pawn);
		}
		SortAssignedPawns();
	}

	public virtual void ForceRemovePawn(Pawn pawn)
	{
		if (assignedPawns.Contains(pawn))
		{
			assignedPawns.Remove(pawn);
		}
		uninstalledAssignedPawns.Remove(pawn);
		SortAssignedPawns();
	}

	public virtual AcceptanceReport CanAssignTo(Pawn pawn)
	{
		return AcceptanceReport.WasAccepted;
	}

	public virtual bool IdeoligionForbids(Pawn pawn)
	{
		return false;
	}

	public virtual void TryAssignPawn(Pawn pawn)
	{
		uninstalledAssignedPawns.Remove(pawn);
		if (!assignedPawns.Contains(pawn))
		{
			assignedPawns.Add(pawn);
			SortAssignedPawns();
		}
	}

	public virtual void TryUnassignPawn(Pawn pawn, bool sort = true, bool uninstall = false)
	{
		if (assignedPawns.Contains(pawn))
		{
			assignedPawns.Remove(pawn);
			if (uninstall && pawn != null && !uninstalledAssignedPawns.Contains(pawn))
			{
				uninstalledAssignedPawns.Add(pawn);
			}
			if (sort)
			{
				SortAssignedPawns();
			}
		}
	}

	public virtual bool AssignedAnything(Pawn pawn)
	{
		return assignedPawns.Contains(pawn);
	}

	protected virtual bool ShouldShowAssignmentGizmo()
	{
		return true;
	}

	protected virtual string GetAssignmentGizmoLabel()
	{
		return "CommandThingSetOwnerLabel".Translate();
	}

	protected virtual string GetAssignmentGizmoDesc()
	{
		return Props.assignmentGizmoDesc;
	}

	public override string CompInspectStringExtra()
	{
		TaggedString taggedString = "";
		if (!assignedPawns.NullOrEmpty())
		{
			taggedString += "Ancot.WorldObject_AssignableToPawn".Translate(assignedPawns.FirstOrDefault());
		}
		else
		{
			taggedString += "Ancot.WorldObject_AssignableToPawn_NoBody".Translate();
		}
		return taggedString;
	}

	public override IEnumerable<Gizmo> GetCaravanGizmos(Caravan caravan)
	{
		if (ShouldShowAssignmentGizmo())
		{
			Command_Action command_Action = new Command_Action
			{
				defaultLabel = GetAssignmentGizmoLabel(),
				icon = AncotLibraryIcon.VanillaAssignOwner,
				defaultDesc = GetAssignmentGizmoDesc(),
				action = delegate
				{
					Find.WindowStack.Add(new Dialog_AssignWorldObjectOwner_Caravan(this, caravan));
				},
				hotKey = KeyBindingDefOf.Misc4
			};
			if (!Props.noAssignablePawnsDesc.NullOrEmpty() && !AssigningCandidates(caravan).Any())
			{
				command_Action.Disable(Props.noAssignablePawnsDesc);
			}
			yield return command_Action;
		}
	}

	public override void PostPostRemove()
	{
		assignedPawns.Clear();
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Collections.Look(ref assignedPawns, "assignedPawns", LookMode.Reference);
		Scribe_Collections.Look(ref uninstalledAssignedPawns, "uninstalledAssignedPawns", LookMode.Reference);
		if (Scribe.mode == LoadSaveMode.PostLoadInit)
		{
			assignedPawns.RemoveAll((Pawn x) => x == null);
			uninstalledAssignedPawns.RemoveAll((Pawn x) => x == null);
		}
	}
}
