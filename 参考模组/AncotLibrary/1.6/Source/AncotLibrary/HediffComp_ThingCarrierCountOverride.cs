using System;
using RimWorld;
using Verse;

namespace AncotLibrary;

public class HediffComp_ThingCarrierCountOverride : HediffComp
{
	private CompThingCarrier_Custom compThingCarrier;

	private HediffCompProperties_ThingCarrierCountOverride Props => (HediffCompProperties_ThingCarrierCountOverride)props;

	public CompThingCarrier_Custom CompThingCarrier => compThingCarrier ?? (compThingCarrier = base.Pawn.TryGetComp<CompThingCarrier_Custom>());

	public override void CompPostMake()
	{
		if (CompThingCarrier != null)
		{
			CompThingCarrier.maxIngredientCountOverride = CompThingCarrier.MaxIngredientCountBase + Props.maxIngredientCountOffset;
			Faction faction = base.Pawn.Faction;
			if (faction != null && !faction.IsPlayer)
			{
				Thing thing = ThingMaker.MakeThing(CompThingCarrier.fixedIngredient);
				thing.stackCount = Props.maxIngredientCountOffset;
				CompThingCarrier.innerContainer.TryAdd(thing, Props.maxIngredientCountOffset);
			}
		}
	}

	public override void CompPostPostRemoved()
	{
		if (CompThingCarrier == null)
		{
			return;
		}
		CompThingCarrier.maxIngredientCountOverride = null;
		if (CompThingCarrier.IngredientCount > CompThingCarrier.MaxIngredientCount)
		{
			int num = CompThingCarrier.IngredientCount - CompThingCarrier.MaxIngredientCount;
			while (num > 0)
			{
				int num2 = Math.Min(num, CompThingCarrier.fixedIngredient.stackLimit);
				Thing thing = ThingMaker.MakeThing(CompThingCarrier.fixedIngredient);
				thing.stackCount = num2;
				GenSpawn.Spawn(thing, base.Pawn.Position, base.Pawn.Map);
				num -= num2;
				CompThingCarrier.TryRemoveThingInCarrier(num2);
			}
		}
		if (CompThingCarrier.maxToFill > CompThingCarrier.MaxIngredientCount)
		{
			CompThingCarrier.maxToFill = CompThingCarrier.MaxIngredientCount;
		}
	}
}
