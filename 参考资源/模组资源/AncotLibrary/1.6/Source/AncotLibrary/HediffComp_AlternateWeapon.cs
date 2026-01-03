using System.Collections.Generic;
using Verse;

namespace AncotLibrary;

public class HediffComp_AlternateWeapon : HediffComp, IThingHolder
{
	public ThingOwner<Thing> innerContainer = new ThingOwner<Thing>();

	private ThingWithComps Weapon => base.Pawn.equipment.Primary;

	private string SwitchLabel => (Props.switchLabel != null) ? Props.switchLabel : ((string)"Ancot.SwitchWeapon".Translate());

	private string SwitchDesc => (Props.switchDesc != null) ? Props.switchDesc : ((string)"Ancot.SwitchWeaponDesc".Translate());

	public IThingHolder ParentHolder => parent.pawn;

	public Thing ContainedThing
	{
		get
		{
			if (!innerContainer.Any)
			{
				return null;
			}
			return innerContainer[0];
		}
	}

	public HediffCompProperties_AlternateWeapon Props => (HediffCompProperties_AlternateWeapon)props;

	public ThingOwner GetDirectlyHeldThings()
	{
		return innerContainer;
	}

	public void GetChildHolders(List<IThingHolder> outChildren)
	{
		ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
	}

	public void AddToContainer(Thing thing)
	{
		thing.DeSpawn();
		innerContainer.TryAddOrTransfer(thing);
	}

	public virtual void EquipeFromStorage()
	{
		if (ContainedThing != null)
		{
			ThingWithComps thingWithComps = innerContainer[0] as ThingWithComps;
			innerContainer.TryDrop(thingWithComps, parent.pawn.Position, parent.pawn.Map, ThingPlaceMode.Near, out var _);
			thingWithComps.DeSpawn();
			parent.pawn.equipment.MakeRoomFor(thingWithComps);
			parent.pawn.equipment.AddEquipment(thingWithComps);
		}
	}

	public override void CompPostTick(ref float severityAdjustment)
	{
		if (ContainedThing != null)
		{
			ContainedThing.DoTick();
		}
	}

	public override IEnumerable<Gizmo> CompGetGizmos()
	{
		yield return new Gizmo_SwitchWeapon_Hediff
		{
			defaultLabel = SwitchLabel,
			defaultDesc = SwitchDesc,
			icon = ((ContainedThing != null) ? ContainedThing.def.uiIcon : AncotLibraryIcon.Gun),
			Order = -99f,
			action = delegate
			{
				Thing containedThing = ContainedThing;
				if (Weapon != null)
				{
					parent.pawn.equipment.TryDropEquipment(Weapon, out var resultingEq, parent.pawn.Position);
					AddToContainer(resultingEq);
				}
				else if (containedThing != null && containedThing is ThingWithComps)
				{
					EquipeFromStorage();
				}
				if (containedThing != null && containedThing is ThingWithComps && innerContainer.Count == 2)
				{
					EquipeFromStorage();
				}
				parent.pawn.Drawer.renderer.renderTree.SetDirty();
			}
		};
	}

	public override void CompPostPostRemoved()
	{
		innerContainer.TryDropAll(parent.pawn.PositionHeld, parent.pawn.Map, ThingPlaceMode.Near);
		parent.pawn.Drawer.renderer.renderTree.SetDirty();
		base.CompPostPostRemoved();
	}

	public override void CompExposeData()
	{
		base.CompExposeData();
		Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
	}
}
