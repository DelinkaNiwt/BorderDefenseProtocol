using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using Verse;

namespace AncotLibrary;

public class RecipeWorker_MakeRandomFittingFromWeapon : RecipeWorker
{
	public override void Notify_IterationCompleted(Pawn billDoer, List<Thing> ingredients)
	{
		foreach (Thing ingredient in ingredients)
		{
			CompUniqueWeapon compUniqueWeapon = ingredient.TryGetComp<CompUniqueWeapon>();
			if (compUniqueWeapon != null)
			{
				FieldInfo field = typeof(Thing).GetField("mapIndexOrState", BindingFlags.Instance | BindingFlags.NonPublic);
				if (field != null)
				{
					Log.Message("field != null");
				}
				sbyte b = (sbyte)Find.Maps.IndexOf(billDoer.Map);
				ingredient.stackCount = 1;
				field?.SetValue(ingredient, b);
				ingredient.DeSpawn();
				GenSpawn.Spawn(ingredient, billDoer.Position, billDoer.Map);
				List<WeaponCategoryDef> weaponCategories = compUniqueWeapon.Props.weaponCategories;
				if (weaponCategories.NullOrEmpty())
				{
					Log.Error("This Weapon's weaponcategory is null");
					break;
				}
				WeaponTraitDef weaponTraitDef = WeaponTraitsUtility.RandomTrait(weaponCategories);
				if (weaponTraitDef == null)
				{
					Log.Error("Cant Find WeaponTraitDef");
				}
				Log.Message(ingredient.Label + weaponTraitDef.defName);
				WeaponTraitsUtility.DropWeaponFitting(weaponTraitDef, billDoer.Position, billDoer.Map);
			}
		}
	}
}
