using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public class CompThingCarrier_Custom : ThingComp, IThingHolder
{
	public int? maxIngredientCountOverride;

	public ThingOwner innerContainer;

	private List<Thing> tmpResources = new List<Thing>();

	public int maxToFill;

	private bool initialized = false;

	public virtual string GizmoDesc => "Ancot.ThingCarrierAutofillDesc".Translate(parent.LabelCap);

	public int MaxIngredientCountBase => Props.maxIngredientCount;

	public virtual int MaxIngredientCount => maxIngredientCountOverride ?? MaxIngredientCountBase;

	public CompProperties_ThingCarrier_Custom Props => (CompProperties_ThingCarrier_Custom)props;

	public ThingDef fixedIngredient => Props.fixedIngredient;

	public int IngredientCount => innerContainer.TotalStackCountOfDef(fixedIngredient);

	public int AmountToAutofill => Mathf.Max(0, maxToFill - IngredientCount);

	public bool LowIngredientCount => IngredientCount < 250;

	public float PercentageFull => (float)IngredientCount / (float)MaxIngredientCount;

	public override void Initialize(CompProperties props)
	{
		base.props = props;
		innerContainer = new ThingOwner<Thing>(this, oneStackOnly: false);
		maxToFill = Props.initialMaxToFillSetting;
	}

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		if (initialized)
		{
			return;
		}
		initialized = true;
		if (!Props.startingLoadForPlayer)
		{
			Faction faction = parent.Faction;
			if (faction == null || faction.IsPlayer)
			{
				return;
			}
		}
		if (Props.startingIngredientCount > 0)
		{
			Thing thing = ThingMaker.MakeThing(fixedIngredient);
			thing.stackCount = Props.startingIngredientCount;
			innerContainer.TryAdd(thing, Props.startingIngredientCount);
		}
	}

	public void TryRemoveThingInCarrier(int num)
	{
		if (num <= 0)
		{
			return;
		}
		tmpResources.Clear();
		tmpResources.AddRange(innerContainer);
		for (int i = 0; i < tmpResources.Count; i++)
		{
			if (num <= 0)
			{
				break;
			}
			int num2 = Mathf.Min(tmpResources[i].stackCount, num);
			if (num2 > 0)
			{
				Thing thing = innerContainer.Take(tmpResources[i], num2);
				if (thing != null)
				{
					num -= thing.stackCount;
					thing.Destroy();
				}
			}
		}
		tmpResources.Clear();
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		int num;
		if (parent != null)
		{
			Faction faction = parent.Faction;
			num = ((faction != null && !faction.IsPlayer) ? 1 : 0);
		}
		else
		{
			num = 1;
		}
		if (num != 0)
		{
			yield break;
		}
		foreach (Gizmo item in base.CompGetGizmosExtra())
		{
			yield return item;
		}
		if (Find.Selector.SingleSelectedThing == parent)
		{
			yield return new ThingCarrierGizmo(this);
		}
		if (!DebugSettings.ShowDevGizmos)
		{
			yield break;
		}
		yield return new Command_Action
		{
			defaultLabel = "DEV: Fill with " + fixedIngredient.label,
			action = delegate
			{
				while (IngredientCount < MaxIngredientCount)
				{
					int stackCount = Mathf.Min(MaxIngredientCount - IngredientCount, fixedIngredient.stackLimit);
					Thing thing = ThingMaker.MakeThing(fixedIngredient);
					thing.stackCount = stackCount;
					innerContainer.TryAdd(thing, thing.stackCount);
				}
			}
		};
		yield return new Command_Action
		{
			defaultLabel = "DEV: Empty " + fixedIngredient.label,
			action = delegate
			{
				innerContainer.ClearAndDestroyContents();
			}
		};
	}

	public void GetChildHolders(List<IThingHolder> outChildren)
	{
		ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
	}

	public ThingOwner GetDirectlyHeldThings()
	{
		return innerContainer;
	}

	public override string CompInspectStringExtra()
	{
		string text = base.CompInspectStringExtra();
		if (!text.NullOrEmpty())
		{
			text += "\n";
		}
		return text + ("CasketContains".Translate() + ": " + innerContainer.ContentsString.CapitalizeFirst());
	}

	public override void PostDestroy(DestroyMode mode, Map previousMap)
	{
		base.PostDestroy(mode, previousMap);
		innerContainer?.ClearAndDestroyContents();
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Deep.Look(ref innerContainer, Props.savePrefix + "innerContainer", this);
		Scribe_Values.Look(ref maxToFill, "maxToFill", 0);
		Scribe_Values.Look(ref maxIngredientCountOverride, "maxIngredientCountOverride");
		Scribe_Values.Look(ref initialized, "initialized", defaultValue: false);
	}

	public override void CompTick()
	{
		base.CompTick();
		if (innerContainer != null)
		{
			innerContainer.DoTick();
		}
	}
}
