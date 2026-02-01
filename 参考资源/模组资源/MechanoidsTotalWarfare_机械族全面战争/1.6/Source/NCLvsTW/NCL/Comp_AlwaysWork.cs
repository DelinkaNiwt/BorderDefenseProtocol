using System.Collections.Generic;
using RimWorld;
using Verse;

namespace NCL;

public class Comp_AlwaysWork : ThingComp
{
	private Pawn Pawn => parent as Pawn;

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		base.PostSpawnSetup(respawningAfterLoad);
		EnsureWorkSettingsInitialized();
	}

	public override void CompTick()
	{
		base.CompTick();
		if (Pawn.IsHashIntervalTick(600))
		{
			EnsureWorkSettingsInitialized();
			ForceAllWorkTypes();
		}
	}

	private void EnsureWorkSettingsInitialized()
	{
		if (Pawn?.workSettings == null && Pawn != null)
		{
			Pawn.workSettings = new Pawn_WorkSettings(Pawn);
			Pawn.workSettings.EnableAndInitialize();
			Log.Message("[AlwaysWork] Initialized work settings for " + Pawn.LabelShort);
		}
	}

	private void ForceAllWorkTypes()
	{
		if (Pawn?.workSettings == null)
		{
			return;
		}
		List<string> allowedWorkTypes = new List<string> { "Construction", "Firefighter", "Hauling", "Cleaning", "Mining", "PlantCutting", "Art", "Crafting" };
		foreach (WorkTypeDef workType in DefDatabase<WorkTypeDef>.AllDefs)
		{
			if (allowedWorkTypes.Contains(workType.defName))
			{
				if (!Pawn.workSettings.WorkIsActive(workType))
				{
					Pawn.workSettings.SetPriority(workType, 3);
				}
			}
			else if (Pawn.workSettings.GetPriority(workType) > 0)
			{
				Pawn.workSettings.SetPriority(workType, 0);
			}
		}
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
	}
}
