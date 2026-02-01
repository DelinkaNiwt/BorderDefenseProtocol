using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded.Chronopath;

public class Ability_Finish : Ability
{
	public override void Cast(params GlobalTargetInfo[] targets)
	{
		((Ability)this).Cast(targets);
		foreach (GlobalTargetInfo globalTargetInfo in targets)
		{
			if (!(globalTargetInfo.Thing is UnfinishedThing unfinishedThing))
			{
				continue;
			}
			List<Thing> ingredients = unfinishedThing.ingredients;
			RecipeDef recipe = unfinishedThing.Recipe;
			ThingDef stuff = unfinishedThing.Stuff;
			IntVec3 position = unfinishedThing.Position;
			Pawn pawn = unfinishedThing.Creator ?? base.pawn;
			List<Thing> list = GenRecipe.MakeRecipeProducts(dominantIngredient: unfinishedThing.def.MadeFromStuff ? ingredients.First((Thing ing) => ing.def == stuff) : (ingredients.NullOrEmpty() ? null : (recipe.productHasIngredientStuff ? ingredients[0] : ((!recipe.products.Any((ThingDefCountClass x) => x.thingDef.MadeFromStuff)) ? ingredients.RandomElementByWeight((Thing x) => x.stackCount) : ingredients.Where((Thing x) => x.def.IsStuff).RandomElementByWeight((Thing x) => x.stackCount)))), recipeDef: recipe, worker: pawn, ingredients: ingredients, billGiver: unfinishedThing.BoundWorkTable as IBillGiver).ToList();
			ingredients.ForEach(delegate(Thing t)
			{
				recipe.Worker.ConsumeIngredient(t, recipe, base.pawn.Map);
			});
			unfinishedThing.BoundBill?.Notify_IterationCompleted(pawn, ingredients);
			recipe.Worker.ConsumeIngredient(unfinishedThing, recipe, base.pawn.Map);
			RecordsUtility.Notify_BillDone(pawn, list);
			if (list.Count == 0)
			{
				break;
			}
			if (recipe.WorkAmountForStuff(stuff) >= 10000f)
			{
				TaleRecorder.RecordTale(TaleDefOf.CompletedLongCraftingProject, pawn, list[0].GetInnerIfMinified().def);
			}
			Find.QuestManager.Notify_ThingsProduced(pawn, list);
			foreach (Thing item in list)
			{
				if (!GenPlace.TryPlaceThing(item, position, base.pawn.Map, ThingPlaceMode.Near))
				{
					Log.Error($"Could not drop recipe product {item} near {position}");
				}
			}
		}
	}

	public override bool CanHitTarget(LocalTargetInfo target)
	{
		if (((Ability)this).CanHitTarget(target))
		{
			return target.Thing is UnfinishedThing;
		}
		return false;
	}
}
