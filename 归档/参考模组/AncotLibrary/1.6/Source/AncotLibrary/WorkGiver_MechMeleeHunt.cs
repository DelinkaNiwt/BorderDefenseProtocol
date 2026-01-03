using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public class WorkGiver_MechMeleeHunt : WorkGiver_Scanner
{
	private bool initialized;

	public ModExtension_MechList Props;

	public readonly HashSet<ThingDef> WhiteListedMechs = new HashSet<ThingDef>();

	public readonly HashSet<ThingDef> WhiteListedMechsRequireWeapon = new HashSet<ThingDef>();

	public override PathEndMode PathEndMode => PathEndMode.OnCell;

	public virtual JobDef Job => AncotJobDefOf.Ancot_MechMeleeHunt;

	private void Init()
	{
		if (initialized)
		{
			return;
		}
		initialized = true;
		Props = def.GetModExtension<ModExtension_MechList>();
		if (Props == null)
		{
			return;
		}
		if (!Props.mechs.NullOrEmpty())
		{
			foreach (ThingDef mech in Props.mechs)
			{
				WhiteListedMechs.Add(mech);
			}
		}
		if (Props.mechsRequireWeapon.NullOrEmpty())
		{
			return;
		}
		foreach (ThingDef item in Props.mechsRequireWeapon)
		{
			WhiteListedMechsRequireWeapon.Add(item);
		}
	}

	public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
	{
		foreach (Designation designation in pawn.Map.designationManager.SpawnedDesignationsOfDef(DesignationDefOf.Hunt))
		{
			yield return designation.target.Thing;
		}
	}

	public override Danger MaxPathDanger(Pawn pawn)
	{
		return Danger.Deadly;
	}

	public override bool ShouldSkip(Pawn pawn, bool forced = false)
	{
		Init();
		if (WhiteListedMechs.Contains(pawn.def))
		{
			return (!HasNoWeapon(pawn) && !HasHuntingWeapon(pawn)) || !pawn.Map.designationManager.AnySpawnedDesignationOfDef(DesignationDefOf.Hunt) || !HealthAllow(pawn);
		}
		if (WhiteListedMechsRequireWeapon.Contains(pawn.def))
		{
			return !HasHuntingWeapon(pawn) || !pawn.Map.designationManager.AnySpawnedDesignationOfDef(DesignationDefOf.Hunt) || !HealthAllow(pawn);
		}
		return true;
	}

	public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
	{
		return t is Pawn pawn2 && pawn2.AnimalOrWildMan() && pawn.CanReserve(t, 1, -1, null, forced) && !t.IsForbidden(pawn) && pawn.Map.designationManager.DesignationOn(t, DesignationDefOf.Hunt) != null && (!HistoryEventUtility.IsKillingInnocentAnimal(pawn, pawn2) || new HistoryEvent(HistoryEventDefOf.KilledInnocentAnimal, pawn.Named(HistoryEventArgsNames.Doer)).Notify_PawnAboutToDo_Job()) && (pawn.Ideo == null || !pawn.Ideo.IsVeneratedAnimal(pawn2) || new HistoryEvent(HistoryEventDefOf.HuntedVeneratedAnimal, pawn.Named(HistoryEventArgsNames.Doer)).Notify_PawnAboutToDo_Job()) && CanFindHuntingPosition(pawn, pawn2);
	}

	public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
	{
		Init();
		return JobMaker.MakeJob(Job, t);
	}

	public static bool HasHuntingWeapon(Pawn p)
	{
		return p.equipment.Primary != null && p.equipment.Primary.def.IsMeleeWeapon && p.equipment.PrimaryEq.PrimaryVerb.HarmsHealth();
	}

	public static bool HasNoWeapon(Pawn p)
	{
		return p.equipment.Primary == null;
	}

	private bool CanFindHuntingPosition(Pawn hunter, Pawn animal)
	{
		return hunter.CanReach(animal.Position, PathEndMode, MaxPathDanger(hunter));
	}

	public bool IsMeleeHunting(Pawn p)
	{
		if (p.CurJobDef != null && p.CurJobDef == Job)
		{
			return true;
		}
		return false;
	}

	public bool HealthAllow(Pawn p)
	{
		if (IsMeleeHunting(p))
		{
			return HealthAllow(p, Props.minHealthPercent);
		}
		return HealthAllow(p, Props.maxHealthPercent);
	}

	public bool HealthAllow(Pawn p, float healthPercent)
	{
		return p.health.summaryHealth.SummaryHealthPercent > healthPercent;
	}
}
