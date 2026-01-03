using System.Collections.Generic;
using Verse;

namespace AncotLibrary;

public class RecipeWorker_CopyWeaponFitting : RecipeWorker
{
	public override void Notify_IterationCompleted(Pawn billDoer, List<Thing> ingredients)
	{
		foreach (Thing ingredient in ingredients)
		{
			if (WeaponTraitsUtility.IsWeaponFitting(ingredient, out var trait))
			{
				WeaponTraitsUtility.DropWeaponFitting(trait, billDoer.Position, billDoer.Map, 2);
				break;
			}
		}
	}
}
