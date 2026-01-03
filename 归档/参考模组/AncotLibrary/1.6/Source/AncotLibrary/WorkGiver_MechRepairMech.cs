using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public class WorkGiver_MechRepairMech : WorkGiver_Scanner
{
	private bool initialized;

	public readonly HashSet<ThingDef> WhiteListedMechs = new HashSet<ThingDef>();

	public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Pawn);

	public override PathEndMode PathEndMode => PathEndMode.Touch;

	private void Init()
	{
		if (initialized)
		{
			return;
		}
		initialized = true;
		ModExtension_MechList modExtension = def.GetModExtension<ModExtension_MechList>();
		if (modExtension == null || modExtension.mechs.NullOrEmpty())
		{
			return;
		}
		foreach (ThingDef mech in modExtension.mechs)
		{
			WhiteListedMechs.Add(mech);
		}
	}

	public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
	{
		return pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction);
	}

	public override Danger MaxPathDanger(Pawn pawn)
	{
		return Danger.Deadly;
	}

	public override bool ShouldSkip(Pawn pawn, bool forced = false)
	{
		Init();
		return !WhiteListedMechs.Contains(pawn.def);
	}

	public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
	{
		if (!ModLister.CheckBiotech("Repair mech"))
		{
			return false;
		}
		Pawn pawn2 = (Pawn)t;
		CompMechRepairable compMechRepairable = t.TryGetComp<CompMechRepairable>();
		return pawn2 != pawn && compMechRepairable != null && pawn2.RaceProps.IsMechanoid && !pawn2.InAggroMentalState && !pawn2.HostileTo(pawn) && pawn.CanReserve(t, 1, -1, null, forced) && !pawn2.IsBurning() && !pawn2.IsAttacking() && pawn2.needs.energy != null && MechRepairUtility.CanRepair(pawn2) && (forced || compMechRepairable.autoRepair);
	}

	public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
	{
		Init();
		return JobMaker.MakeJob(AncotJobDefOf.Ancot_MechRepairMech, t);
	}
}
