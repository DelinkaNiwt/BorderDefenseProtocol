using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace NiceInventoryTab;

internal static class WorkStatUtility
{
	internal static PropertyInfo JobDriver_AffectFloor_SpeedStat = typeof(JobDriver_AffectFloor).GetProperty("SpeedStat", BindingFlags.Instance | BindingFlags.NonPublic);

	internal static PropertyInfo JobDriver_Research_ResearchBench = typeof(JobDriver_Research).GetProperty("ResearchBench", BindingFlags.Instance | BindingFlags.NonPublic);

	internal static float MaxWorkEfficiency(Pawn pawn)
	{
		return 1.8f;
	}

	internal static StatDef GetStat(Pawn pawn, JobDriver driver, out SkillDef activeSkill, out string overrideName)
	{
		activeSkill = null;
		overrideName = null;
		if (driver is JobDriver_AffectFloor obj)
		{
			activeSkill = SkillDefOf.Construction;
			overrideName = "NIT_ConstructionWork".Translate();
			return (StatDef)JobDriver_AffectFloor_SpeedStat.GetValue(obj);
		}
		if (driver is JobDriver_DoBill)
		{
			RecipeDef recipeDef = pawn.CurJob.RecipeDef;
			if (recipeDef != null)
			{
				activeSkill = recipeDef.workSkill;
				if (recipeDef.workSkill == SkillDefOf.Construction)
				{
					overrideName = "NIT_ConstructionWork".Translate();
				}
				else if (recipeDef.workSkill == SkillDefOf.Mining)
				{
					overrideName = "NIT_MiningWork".Translate();
				}
				else if (recipeDef.workSkill == SkillDefOf.Plants)
				{
					overrideName = "NIT_PlantsWork".Translate();
				}
				else if (recipeDef.workSkill == SkillDefOf.Animals)
				{
					overrideName = "NIT_AnimalsWork".Translate();
				}
				else if (recipeDef.workSkill == SkillDefOf.Cooking)
				{
					overrideName = "NIT_CookingWork".Translate();
				}
				else if (recipeDef.workSkill == SkillDefOf.Crafting)
				{
					overrideName = "NIT_CraftingWork".Translate();
				}
				else if (recipeDef.workSkill == SkillDefOf.Artistic)
				{
					overrideName = "NIT_ArtisticWork".Translate();
				}
				else if (recipeDef.workSkill == SkillDefOf.Medicine)
				{
					overrideName = "NIT_MedicalWork".Translate();
				}
				else if (recipeDef.workSkill == SkillDefOf.Intellectual)
				{
					overrideName = "NIT_IntellectualWork".Translate();
				}
			}
			return pawn.CurJob.RecipeDef?.workSpeedStat;
		}
		if (driver is JobDriver_AffectRoof || driver is JobDriver_ConstructFinishFrame || driver is JobDriver_RemoveBuilding || driver is JobDriver_SmoothWall || driver is JobDriver_BuildCubeSculpture || driver is JobDriver_BuildSnowman)
		{
			activeSkill = SkillDefOf.Construction;
			overrideName = "NIT_ConstructionWork".Translate();
			return StatDefOf.ConstructionSpeed;
		}
		if (driver is JobDriver_Repair)
		{
			activeSkill = SkillDefOf.Construction;
			overrideName = "NIT_RepairWork".Translate();
			return StatDefOf.ConstructionSpeed;
		}
		if (driver is JobDriver_RepairMech || driver is JobDriver_RepairMechRemote)
		{
			activeSkill = SkillDefOf.Crafting;
			overrideName = "NIT_RepairWork".Translate();
			return StatDefOf.MechRepairSpeed;
		}
		if (driver is JobDriver_Mine)
		{
			activeSkill = SkillDefOf.Mining;
			overrideName = "NIT_MiningWork".Translate();
			return StatDefOf.MiningSpeed;
		}
		if (driver is JobDriver_CleanFilth || driver is JobDriver_ClearPollution)
		{
			overrideName = "NIT_ClearWork".Translate();
			activeSkill = null;
			if (!(driver is JobDriver_ClearPollution))
			{
				return StatDefOf.CleaningSpeed;
			}
			return StatDefOf.GeneralLaborSpeed;
		}
		if (driver is JobDriver_Ingest || driver is JobDriver_PredatorHunt)
		{
			overrideName = "NIT_EatWork".Translate();
			activeSkill = null;
			return StatDefOf.EatingSpeed;
		}
		if (driver is JobDriver_PlantSeed || driver is JobDriver_PlantSow || driver is JobDriver_PlantWork || driver is JobDriver_PruneGauranlenTre)
		{
			activeSkill = SkillDefOf.Plants;
			overrideName = "NIT_PlantsWork".Translate();
			if (!(driver is JobDriver_PruneGauranlenTre))
			{
				return StatDefOf.PlantWorkSpeed;
			}
			return StatDefOf.PruningSpeed;
		}
		if (driver is JobDriver_GatherAnimalBodyResources)
		{
			activeSkill = SkillDefOf.Animals;
			overrideName = "NIT_AnimalsWork".Translate();
			return StatDefOf.AnimalGatherSpeed;
		}
		if (driver is JobDriver_ExtractBioferrite || driver is JobDriver_TendPatient || driver is JobDriver_TendEntity)
		{
			activeSkill = SkillDefOf.Medicine;
			overrideName = "NIT_MedicalWork".Translate();
			return StatDefOf.MedicalTendSpeed;
		}
		if (driver is JobDriver_CreateXenogerm || driver is JobDriver_Research || driver is JobDriver_StudyItem || driver is JobDriver_StudyInteract || driver is JobDriver_Hack || driver is JobDriver_AnalyzeItem || driver is JobDriver_OperateScanner)
		{
			activeSkill = SkillDefOf.Intellectual;
			overrideName = "NIT_IntellectualWork".Translate();
			if (driver is JobDriver_Hack)
			{
				return StatDefOf.HackingSpeed;
			}
			if (driver is JobDriver_StudyInteract)
			{
				return StatDefOf.EntityStudyRate;
			}
			return StatDefOf.ResearchSpeed;
		}
		if (driver is JobDriver_PaintBuilding || driver is JobDriver_PaintFloor || driver is JobDriver_RemovePaintBuilding || driver is JobDriver_RemovePaintFloor)
		{
			activeSkill = SkillDefOf.Artistic;
			overrideName = "NIT_ArtisticWork".Translate();
			return StatDefOf.WorkSpeedGlobal;
		}
		return null;
	}

	internal static Thing Workbench(Job curJob, JobDriver driver, out float factor)
	{
		factor = 1f;
		if (curJob == null)
		{
			return null;
		}
		if (driver == null)
		{
			return null;
		}
		if (driver is JobDriver_DoBill { BillGiver: Building_WorkTable billGiver })
		{
			if (billGiver != null && curJob.RecipeDef != null && curJob.RecipeDef.workTableSpeedStat != null)
			{
				factor = billGiver.GetStatValue(curJob.RecipeDef.workTableSpeedStat);
			}
			return billGiver;
		}
		if (driver is JobDriver_Research)
		{
			Building_ResearchBench building_ResearchBench = (Building_ResearchBench)JobDriver_Research_ResearchBench.GetValue(driver);
			factor = building_ResearchBench?.GetStatValue(StatDefOf.ResearchSpeedFactor) ?? 1f;
			return building_ResearchBench;
		}
		return null;
	}

	internal static float WorkEfficiency(Pawn pawn, StatDrawer drawer)
	{
		StatDef statDef = null;
		JobDriver jobDriver = pawn.CurJob?.GetCachedDriver(pawn);
		if (jobDriver != null)
		{
			statDef = GetStat(pawn, jobDriver, out var activeSkill, out var overrideName);
			if (drawer != null)
			{
				if (!overrideName.NullOrEmpty())
				{
					drawer.Title = overrideName;
				}
				else if (jobDriver.ActiveSkill != null)
				{
					drawer.Title = jobDriver.ActiveSkill.LabelCap;
				}
				else if (activeSkill != null)
				{
					drawer.Title = activeSkill.LabelCap;
				}
				else
				{
					drawer.Title = "NIT_WorkEfficiency".Translate();
				}
			}
		}
		float num = CommonStatUtility.SolveStat(pawn, drawer, statDef ?? StatDefOf.WorkSpeedGlobal, medCheck: true, checkLight: true);
		float factor;
		Thing thing = Workbench(pawn.CurJob, jobDriver, out factor);
		if (thing != null)
		{
			float f = num;
			float num2 = num * factor - num;
			num *= factor;
			if (Mathf.Abs(num2) > 0.01f)
			{
				(drawer as StatBar).AddAutoBuffDebuff(num2, (drawer as StatBar).ColorBar, Assets.EnviromentPenaltyColor);
			}
			string[] array = drawer.Descr.Split('\n');
			if (array.Length > 1)
			{
				drawer.Descr = string.Join("\n", array, 0, array.Length - 1);
			}
			StatDrawer statDrawer = drawer;
			statDrawer.Descr = statDrawer.Descr + "\n" + pawn.LabelShortCap + ": " + f.ToStringPercent();
			statDrawer = drawer;
			statDrawer.Descr = statDrawer.Descr + "\n" + thing.LabelCap + ": x" + factor.ToStringPercent();
			drawer.Descr += "\n\n" + "StatsReport_FinalValue".Translate() + ": " + num.ToStringPercent();
		}
		return num;
	}
}
