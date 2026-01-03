using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AncotLibrary;

public class RecipeWorker_MakeRandomFittings : RecipeWorker
{
	public override void Notify_IterationCompleted(Pawn billDoer, List<Thing> ingredients)
	{
		WeaponTraitDef weaponTraitDef = WeaponTraitsUtility.RandomTraitExceptBladeLink();
		if (weaponTraitDef != null)
		{
			WeaponTraitsUtility.DropWeaponFitting(weaponTraitDef, billDoer.Position, billDoer.Map);
		}
	}
}
