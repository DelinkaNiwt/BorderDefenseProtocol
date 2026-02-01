using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace NCL;

public class CompSteelResource : ThingComp, IThingHolder
{
	public ThingOwner innerContainer;

	public int maxToFill;

	public bool autoFill = true;

	public bool AutoFill
	{
		get
		{
			return autoFill;
		}
		set
		{
			autoFill = value;
		}
	}

	public CompProperties_SteelResource Props => (CompProperties_SteelResource)props;

	public int IngredientCount => innerContainer?.TotalStackCountOfDef(Props.fixedIngredient) ?? 0;

	public int AmountToAutofill => Mathf.Max(0, maxToFill - IngredientCount);

	public float FillPercentage => (float)IngredientCount / (float)Props.maxIngredientCount;

	public int MaxToFill
	{
		get
		{
			return maxToFill;
		}
		set
		{
			maxToFill = Mathf.Clamp(value, 0, Props.maxIngredientCount);
		}
	}

	public bool HasEnoughResources(int amount)
	{
		return IngredientCount >= amount;
	}

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		base.PostSpawnSetup(respawningAfterLoad);
		if (!respawningAfterLoad && innerContainer == null)
		{
			innerContainer = new ThingOwner<Thing>(this);
			if (Props.startingIngredientCount > 0)
			{
				AddIngredient(Props.fixedIngredient, Props.startingIngredientCount);
			}
			maxToFill = Props.startingIngredientCount;
		}
	}

	public void AddIngredient(ThingDef ingredientDef, int amount)
	{
		if (innerContainer != null)
		{
			int num = Mathf.Min(amount, Props.maxIngredientCount - IngredientCount);
			if (num > 0)
			{
				Thing thing = ThingMaker.MakeThing(ingredientDef);
				thing.stackCount = num;
				innerContainer.TryAdd(thing);
			}
		}
	}

	public bool ConsumeResources(int amount)
	{
		int num = amount;
		List<Thing> list = new List<Thing>(innerContainer);
		foreach (Thing thing in list)
		{
			if (thing.def == Props.fixedIngredient && thing.stackCount > 0)
			{
				int num2 = Mathf.Min(thing.stackCount, num);
				if (num2 >= thing.stackCount)
				{
					innerContainer.Remove(thing);
					thing.Destroy();
				}
				else
				{
					thing.SplitOff(num2)?.Destroy();
				}
				num -= num2;
				if (num <= 0)
				{
					break;
				}
			}
		}
		return num <= 0;
	}

	public void EjectResources()
	{
		if (innerContainer != null && innerContainer.Count != 0)
		{
			innerContainer.TryDropAll(parent.Position, parent.Map, ThingPlaceMode.Near);
			maxToFill = 0;
		}
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
		Scribe_Values.Look(ref maxToFill, "maxToFill", 0);
		Scribe_Values.Look(ref autoFill, "autoFill", defaultValue: true);
	}

	public void GetChildHolders(List<IThingHolder> outChildren)
	{
		ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
	}

	public ThingOwner GetDirectlyHeldThings()
	{
		return innerContainer;
	}

	public override void PostDestroy(DestroyMode mode, Map previousMap)
	{
		base.PostDestroy(mode, previousMap);
		innerContainer?.ClearAndDestroyContents();
	}
}
