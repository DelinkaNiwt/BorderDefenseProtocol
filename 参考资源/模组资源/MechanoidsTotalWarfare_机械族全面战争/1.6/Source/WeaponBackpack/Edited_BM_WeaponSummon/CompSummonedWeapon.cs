using System.Linq;
using RimWorld;
using Verse;

namespace Edited_BM_WeaponSummon;

public class CompSummonedWeapon : ThingComp
{
	public int ticksSummoned;

	private int lastTicked;

	public CompProperties_SummonedWeapon Props => props as CompProperties_SummonedWeapon;

	public override void CompTick()
	{
		base.CompTick();
		if (lastTicked == Find.TickManager.TicksGame)
		{
			return;
		}
		lastTicked = Find.TickManager.TicksGame;
		Pawn_EquipmentTracker pawn_EquipmentTracker = parent.ParentHolder as Pawn_EquipmentTracker;
		if (Find.TickManager.TicksGame - ticksSummoned >= Props.lifetimeDuration || pawn_EquipmentTracker == null)
		{
			Map mapHeld = parent.MapHeld;
			if (mapHeld != null && Props.fleckWhenExpired != null)
			{
				FleckMaker.Static(parent.PositionHeld, mapHeld, Props.fleckWhenExpired);
			}
			parent.Destroy();
			if (pawn_EquipmentTracker != null && pawn_EquipmentTracker.pawn.inventory.innerContainer.Where((Thing x) => x.def.IsWeapon).FirstOrDefault() is ThingWithComps { holdingOwner: var holdingOwner } thingWithComps)
			{
				holdingOwner?.Remove(thingWithComps);
				pawn_EquipmentTracker.AddEquipment(thingWithComps);
			}
		}
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref ticksSummoned, "ticksSummoned", 0);
	}
}
