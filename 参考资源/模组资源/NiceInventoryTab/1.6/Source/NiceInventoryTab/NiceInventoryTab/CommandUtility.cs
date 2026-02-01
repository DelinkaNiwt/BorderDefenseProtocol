using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace NiceInventoryTab;

public static class CommandUtility
{
	public static bool CanControl(Pawn pawn)
	{
		if (!pawn.Spawned)
		{
			return false;
		}
		if (pawn.Downed || pawn.InMentalState || pawn.CarriedBy != null)
		{
			return false;
		}
		if (pawn.Faction != Faction.OfPlayer && !pawn.IsPrisonerOfColony)
		{
			return false;
		}
		if (pawn.IsPrisonerOfColony && pawn.Spawned && !pawn.Map.mapPawns.AnyFreeColonistSpawned)
		{
			return false;
		}
		if (pawn.IsPrisonerOfColony && (PrisonBreakUtility.IsPrisonBreaking(pawn) || (pawn.CurJob != null && pawn.CurJob.exitMapOnArrival)))
		{
			return false;
		}
		if (pawn.Dead)
		{
			return false;
		}
		return true;
	}

	public static bool CanControlColonist(Pawn pawn)
	{
		if (CanControl(pawn))
		{
			return pawn.IsColonistPlayerControlled;
		}
		return false;
	}

	public static void CommandCreate(Building_WorkTable wtable, ThingDef appdef, RecipeDef recipe, ApparelLayerDef apparelLayer)
	{
		if (wtable != null && recipe != null && appdef != null)
		{
			Bill bill = recipe.MakeNewBill();
			if (bill is Bill_ProductionWithUft bill2)
			{
				BillFinished_Patch.Register(bill2, ITab_Pawn_Gear_Patch.lastPawn, apparelLayer);
			}
			if (Settings.AutoRenameBillLabel && bill is Bill_Production bill_Production)
			{
				bill_Production.RenamableLabel = "NIT_AutoNameRecipe".Translate(appdef.LabelCap, ITab_Pawn_Gear_Patch.lastPawn.LabelShortCap);
			}
			wtable.billStack.AddBill(bill);
			Messages.Message("NIT_BillCreatedExpl".Translate(wtable.LabelCap, ITab_Pawn_Gear_Patch.lastPawn.NameShortColored), MessageTypeDefOf.TaskCompletion, historical: false);
			if (wtable.Map == Find.CurrentMap)
			{
				CameraJumper.TryJump(wtable);
			}
		}
	}

	public static void CommandWear(Pawn pawn, Thing app)
	{
		Job job = JobMaker.MakeJob(JobDefOf.Wear, app);
		job.playerForced = true;
		pawn.jobs.StartJob(job, JobCondition.InterruptForced, null, resumeCurJobAfterwards: true);
		if (pawn.Map == Find.CurrentMap)
		{
			CameraJumper.TryJump(app);
		}
	}

	public static void CommandRemoveToInventory(Pawn pawn, Thing app, bool start = true)
	{
		Job job = JobMaker.MakeJob(Assets.NIT_MoveApparelToInventory, app);
		job.playerForced = true;
		if (start)
		{
			pawn.jobs.StopAll();
		}
		pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc, requestQueueing: true);
	}

	public static void CommandWearFromInventory(Pawn pawn, Thing app)
	{
		pawn.jobs.StopAll();
		List<Apparel> wornApparel = pawn.apparel.WornApparel;
		for (int num = wornApparel.Count - 1; num >= 0; num--)
		{
			if (!ApparelUtility.CanWearTogether(app.def, wornApparel[num].def, pawn.RaceProps.body))
			{
				CommandRemoveToInventory(pawn, wornApparel[num], start: false);
			}
		}
		Job job = JobMaker.MakeJob(Assets.NIT_WearFromInventory, app);
		job.playerForced = true;
		pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc, requestQueueing: true);
	}

	public static void CommandDrop(Pawn pawn, Thing t)
	{
		ThingWithComps thingWithComps = t as ThingWithComps;
		if (t is Apparel apparel && pawn.apparel != null && pawn.apparel.WornApparel.Contains(apparel))
		{
			pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.RemoveApparel, apparel), JobTag.Misc);
		}
		else if (thingWithComps != null && pawn.equipment != null && pawn.equipment.AllEquipmentListForReading.Contains(thingWithComps))
		{
			pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.DropEquipment, thingWithComps), JobTag.Misc);
		}
		else if (!t.def.destroyOnDrop)
		{
			pawn.inventory.innerContainer.TryDrop(t, pawn.Position, pawn.Map, ThingPlaceMode.Near, out var _);
		}
	}

	private static ThingOwner GetContainer(Pawn pawn)
	{
		if (pawn.inventory != null)
		{
			return pawn.inventory.innerContainer;
		}
		return null;
	}

	public static bool CanFitInInventory(Pawn pawn, ThingDef thingDef, out int count, bool ignoreEquipment = false, bool useApparelCalculations = false)
	{
		float statValueAbstract;
		if (useApparelCalculations)
		{
			statValueAbstract = thingDef.GetStatValueAbstract(StatDefOf.Mass);
			if (statValueAbstract <= 0f)
			{
				count = 1;
				return true;
			}
		}
		else
		{
			statValueAbstract = thingDef.GetStatValueAbstract(StatDefOf.Mass);
		}
		if (MassUtility.FreeSpace(pawn) > statValueAbstract)
		{
			count = 1;
			return true;
		}
		count = 0;
		return false;
	}

	public static void GetEquipmentStats(ThingWithComps eq, out float weight)
	{
		weight = eq.GetStatValue(StatDefOf.Mass);
	}

	public static void CommandEquipWeaponFromInventory(Pawn pawn, ThingWithComps item)
	{
		pawn.jobs?.StopAll();
		if (pawn.equipment.Primary != null)
		{
			if (CanFitInInventory(pawn, pawn.equipment.Primary.def, out var _, ignoreEquipment: true))
			{
				pawn.equipment.TryTransferEquipmentToContainer(pawn.equipment.Primary, GetContainer(pawn));
			}
			else
			{
				pawn.equipment.MakeRoomFor(item);
			}
		}
		pawn.equipment.AddEquipment((ThingWithComps)GetContainer(pawn).Take(item, 1));
		item.def.soundInteract?.PlayOneShot(new TargetInfo(pawn.Position, pawn.MapHeld));
	}

	public static void CommandMoveWeaponToInventory(Pawn pawn, ThingWithComps item)
	{
		if (CanFitInInventory(pawn, pawn.equipment.Primary.def, out var _, ignoreEquipment: true))
		{
			pawn.equipment.TryTransferEquipmentToContainer(pawn.equipment.Primary, GetContainer(pawn));
		}
		else
		{
			pawn.equipment.MakeRoomFor(item);
		}
	}
}
