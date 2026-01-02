using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test;

public class CompAutoRepair : ThingComp
{
	public int ticksPassedSinceLastHeal;

	public CompProperties_AutoRepair Props => (CompProperties_AutoRepair)props;

	public override void PostExposeData()
	{
		Scribe_Values.Look(ref ticksPassedSinceLastHeal, "ticksPassedSinceLastHeal", 0);
	}

	public override void CompTick()
	{
		Tick(1);
	}

	public override void CompTickRare()
	{
		Tick(250);
	}

	public override void CompTickLong()
	{
		Tick(1200);
	}

	public void Tick(int ticks)
	{
		ticksPassedSinceLastHeal += ticks;
		if (ticksPassedSinceLastHeal < Props.ticksPerHeal)
		{
			return;
		}
		ticksPassedSinceLastHeal = 0;
		if (parent is Apparel)
		{
			Apparel apparel = (Apparel)parent;
			Pawn wearer = apparel.Wearer;
			if (wearer != null)
			{
				TryHealAllEquipment(wearer);
			}
		}
	}

	public void TryHealAllEquipment(Pawn pawn)
	{
		Pawn_ApparelTracker apparel = pawn.apparel;
		Pawn_EquipmentTracker equipment = pawn.equipment;
		ThingWithComps primary = pawn.equipment.Primary;
		List<Apparel> list = apparel?.WornApparel;
		List<ThingWithComps> list2 = equipment?.AllEquipmentListForReading;
		if (!list.NullOrEmpty())
		{
			foreach (Apparel item in list)
			{
				TryRepair(item);
			}
		}
		if (!list2.NullOrEmpty())
		{
			foreach (Apparel item2 in list)
			{
				TryRepair(item2);
			}
		}
		if (primary != null)
		{
			TryRepair(primary);
		}
	}

	public void TryRepair(Thing thing)
	{
		if (thing.def.useHitPoints)
		{
			if (thing.HitPoints < thing.MaxHitPoints)
			{
				thing.HitPoints += Mathf.Max(5, Mathf.CeilToInt((float)thing.MaxHitPoints * 0.01f));
				thing.HitPoints = Mathf.Min(thing.HitPoints, thing.MaxHitPoints);
			}
			else if (thing.HitPoints >= thing.MaxHitPoints)
			{
				thing.HitPoints = thing.MaxHitPoints;
			}
		}
	}
}
