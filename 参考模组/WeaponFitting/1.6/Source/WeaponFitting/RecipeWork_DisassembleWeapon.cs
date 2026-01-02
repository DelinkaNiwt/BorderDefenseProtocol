using System.Collections.Generic;
using AncotLibrary;
using RimWorld;
using Verse;

namespace WeaponFitting;

public class RecipeWork_DisassembleWeapon : RecipeWorker
{
	public override void Notify_IterationCompleted(Pawn billDoer, List<Thing> ingredients)
	{
		CompUniqueWeapon comp = new CompUniqueWeapon();
		Thing weapon = new Thing();
		foreach (Thing thing in ingredients)
		{
			CompUniqueWeapon compUnique = thing.TryGetComp<CompUniqueWeapon>();
			if (compUnique != null)
			{
				comp = compUnique;
				weapon = thing;
				break;
			}
		}
		foreach (WeaponTraitDef traitDef in comp.TraitsListForReading)
		{
			WeaponTraitsUtility.DropWeaponFitting(traitDef, billDoer.Position, billDoer.Map);
		}
		if (!WF_DefOf.WeaponFittings_II.IsFinished)
		{
			return;
		}
		List<ThingDefCountClass> costListAdj = weapon.def.CostListAdjusted(weapon.Stuff);
		foreach (ThingDefCountClass defCountClass in costListAdj)
		{
			int num = GenMath.RoundRandom((float)defCountClass.count * 0.4f);
			if (num > 0)
			{
				Thing thing2 = ThingMaker.MakeThing(defCountClass.thingDef);
				thing2.stackCount = num;
				GenPlace.TryPlaceThing(thing2, billDoer.Position, billDoer.Map, ThingPlaceMode.Near, null, null, null, 2);
			}
		}
	}
}
