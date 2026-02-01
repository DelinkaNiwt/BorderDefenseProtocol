using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace NiceInventoryTab;

[HarmonyPatch(typeof(Toils_Recipe), "FinishRecipeAndStartStoringProduct")]
internal class BillFinished_Patch
{
	private class WearRequest
	{
		public Pawn TargetPawn;

		public ApparelLayerDef Layer;
	}

	private static readonly Dictionary<Bill_ProductionWithUft, WearRequest> wearRequests = new Dictionary<Bill_ProductionWithUft, WearRequest>();

	private static readonly MethodInfo mi_CalculateIngredients = typeof(Toils_Recipe).GetMethod("CalculateIngredients", BindingFlags.Static | BindingFlags.NonPublic);

	private static readonly MethodInfo mi_CalculateDominantIngredient = typeof(Toils_Recipe).GetMethod("CalculateDominantIngredient", BindingFlags.Static | BindingFlags.NonPublic);

	private static readonly MethodInfo mi_ConsumeIngredients = typeof(Toils_Recipe).GetMethod("ConsumeIngredients", BindingFlags.Static | BindingFlags.NonPublic);

	public static void Clear()
	{
		wearRequests.Clear();
	}

	public static void Register(Bill_ProductionWithUft bill, Pawn targetPawn, ApparelLayerDef layer)
	{
		if (bill != null && targetPawn != null && layer != null)
		{
			wearRequests[bill] = new WearRequest
			{
				TargetPawn = targetPawn,
				Layer = layer
			};
		}
	}

	public static void Unregister(Bill_ProductionWithUft bill)
	{
		if (bill != null)
		{
			wearRequests.Remove(bill);
		}
	}

	public static bool TryGetExistingWearBill(Pawn targetPawn, ApparelLayerDef layer, out Bill_ProductionWithUft existingBill)
	{
		existingBill = null;
		if (targetPawn == null || layer == null)
		{
			return false;
		}
		foreach (KeyValuePair<Bill_ProductionWithUft, WearRequest> wearRequest in wearRequests)
		{
			Bill_ProductionWithUft key = wearRequest.Key;
			WearRequest value = wearRequest.Value;
			if (!key.deleted && value.TargetPawn == targetPawn && value.Layer == layer)
			{
				existingBill = key;
				return true;
			}
		}
		return false;
	}

	private static void Postfix(ref Toil __result)
	{
		if (!Settings.EnableAutoWear)
		{
			return;
		}
		Action originalInitAction = __result.initAction;
		Toil toil = __result;
		__result.initAction = delegate
		{
			Pawn actor = toil.actor;
			WearRequest value;
			if (actor == null || actor.jobs?.curDriver == null)
			{
				originalInitAction?.Invoke();
			}
			else if (!(actor.jobs.curDriver is JobDriver_DoBill jobDriver_DoBill))
			{
				originalInitAction?.Invoke();
			}
			else if (jobDriver_DoBill.job.bill is Bill_ProductionWithUft key && wearRequests.TryGetValue(key, out value) && value != null)
			{
				wearRequests.Remove(key);
				Pawn targetPawn = value.TargetPawn;
				if (targetPawn == null || targetPawn.Dead || targetPawn.Destroyed || !targetPawn.IsColonistPlayerControlled)
				{
					Log.Warning(ModIntegration.ModLogPrefix + " Целевая пешка невалидна — выполняем стандартное завершение");
					originalInitAction?.Invoke();
				}
				else
				{
					ExecuteFinishRecipeLogic(actor, jobDriver_DoBill, targetPawn);
					actor.jobs.EndCurrentJob(JobCondition.Succeeded);
				}
			}
			else
			{
				originalInitAction?.Invoke();
			}
		};
	}

	private static void ExecuteFinishRecipeLogic(Pawn crafter, JobDriver_DoBill driver, Pawn target)
	{
		Job curJob = crafter.jobs.curJob;
		if (curJob?.RecipeDef == null)
		{
			return;
		}
		List<Thing> list = CalculateIngredients(curJob, crafter);
		if (list == null)
		{
			Log.Error(ModIntegration.ModLogPrefix + " CalculateIngredients вернул null!");
			return;
		}
		Thing dominantIngredient = CalculateDominantIngredient(curJob, list);
		ThingStyleDef style = null;
		if (ModsConfig.IdeologyActive)
		{
			List<ThingDefCountClass> products = curJob.bill.recipe.products;
			if (products != null && products.Count == 1)
			{
				style = ((!curJob.bill.globalStyle) ? curJob.bill.style : Faction.OfPlayer.ideos.PrimaryIdeo.style.StyleForThingDef(curJob.bill.recipe.ProducedThingDef)?.styleDef);
			}
		}
		List<Thing> list2 = GenRecipe.MakeRecipeProducts(curJob.RecipeDef, crafter, list, dominantIngredient, driver.BillGiver, curJob.bill.precept, style, curJob.bill.graphicIndexOverride).ToList();
		ConsumeIngredients(list, curJob.RecipeDef, crafter.Map);
		curJob.bill.Notify_IterationCompleted(crafter, list);
		RecordsUtility.Notify_BillDone(crafter, list2);
		Find.QuestManager.Notify_ThingsProduced(crafter, list2);
		if (list2.Count > 0 && list2[0] is Apparel { WornByCorpse: false } apparel)
		{
			if (target.apparel.CanWearWithoutDroppingAnything(apparel.def))
			{
				if (GenPlace.TryPlaceThing(apparel, crafter.Position, crafter.Map, ThingPlaceMode.Near))
				{
					Job job = JobMaker.MakeJob(JobDefOf.Wear, apparel);
					job.count = 1;
					job.playerForced = true;
					target.jobs.TryTakeOrderedJob(job, JobTag.Misc, requestQueueing: true);
					Messages.Message("NIT_PawnAutoWearExpl".Translate(target.NameShortColored, apparel.LabelShort), MessageTypeDefOf.TaskCompletion, historical: false);
				}
			}
			else
			{
				GenPlace.TryPlaceThing(apparel, crafter.Position, crafter.Map, ThingPlaceMode.Near);
			}
		}
		else
		{
			if (list2.Count <= 0)
			{
				return;
			}
			foreach (Thing item in list2)
			{
				GenPlace.TryPlaceThing(item, crafter.Position, crafter.Map, ThingPlaceMode.Near);
			}
		}
	}

	public static List<Thing> CalculateIngredients(Job job, Pawn actor)
	{
		if (!(mi_CalculateIngredients.Invoke(null, new object[2] { job, actor }) is List<Thing> result))
		{
			return null;
		}
		return result;
	}

	public static Thing CalculateDominantIngredient(Job job, List<Thing> ingredients)
	{
		return (Thing)mi_CalculateDominantIngredient.Invoke(null, new object[2] { job, ingredients });
	}

	public static void ConsumeIngredients(List<Thing> ingredients, RecipeDef recipe, Map map)
	{
		mi_ConsumeIngredients.Invoke(null, new object[3] { ingredients, recipe, map });
	}
}
