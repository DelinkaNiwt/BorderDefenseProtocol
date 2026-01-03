using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public class WorkGiver_MechDeepDrill : WorkGiver_Scanner
{
	private bool initialized;

	public readonly HashSet<ThingDef> WhiteListedMechs = new HashSet<ThingDef>();

	public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(ThingDefOf.DeepDrill);

	public override PathEndMode PathEndMode => PathEndMode.InteractionCell;

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

	public override Danger MaxPathDanger(Pawn pawn)
	{
		return Danger.Deadly;
	}

	public override bool ShouldSkip(Pawn pawn, bool forced = false)
	{
		Init();
		if (!WhiteListedMechs.Contains(pawn.def))
		{
			return true;
		}
		List<Thing> list = pawn.Map.listerThings.ThingsOfDef(ThingDefOf.DeepDrill);
		return list.Count == 0;
	}

	public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
	{
		if (t.Faction != pawn.Faction)
		{
			return false;
		}
		if (!(t is Building building))
		{
			return false;
		}
		if (building.IsForbidden(pawn))
		{
			return false;
		}
		if (!pawn.CanReserve(building, 1, -1, null, forced))
		{
			return false;
		}
		if (!building.TryGetComp<CompDeepDrill>().CanDrillNow())
		{
			return false;
		}
		if (building.Map.designationManager.DesignationOn(building, DesignationDefOf.Uninstall) != null)
		{
			return false;
		}
		if (building.IsBurning())
		{
			return false;
		}
		return true;
	}

	public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
	{
		Init();
		return JobMaker.MakeJob(AncotJobDefOf.Ancot_MechOperateDeepDrill, t, 1500, checkOverrideOnExpiry: true);
	}
}
