using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace NCL;

public class CompLightningOnDestroy : ThingComp
{
	public CompProperties_LightningOnDestroy Props => (CompProperties_LightningOnDestroy)props;

	public override void PostDestroy(DestroyMode mode, Map previousMap)
	{
		base.PostDestroy(mode, previousMap);
		if (previousMap == null)
		{
			return;
		}
		List<Pawn> mechWorms = (from Pawn p in from t in previousMap.listerThings.ThingsOfDef(Props.mechWormDef)
				where !t.Destroyed
				select t
			where p.Spawned && !p.Dead
			select p).ToList();
		if (mechWorms.Count == 0)
		{
			return;
		}
		Faction wormFaction = mechWorms[0].Faction;
		if (wormFaction == null)
		{
			return;
		}
		List<IntVec3> targets = FindTargetCells(previousMap, wormFaction);
		if (targets.Count == 0)
		{
			return;
		}
		foreach (IntVec3 target in targets)
		{
			DoLightningStrike(target, previousMap);
		}
	}

	private List<IntVec3> FindTargetCells(Map map, Faction wormFaction)
	{
		return (from p in (from p in (from c in GenRadial.RadialCellsAround(parent.Position, Props.strikeRange, useCenter: true)
					where c.InBounds(map)
					select c).SelectMany((IntVec3 c) => c.GetThingList(map)).OfType<Pawn>()
				where p.Spawned && !p.Dead && p.Faction != null && p.Faction.HostileTo(wormFaction)
				select p).Distinct().Take(Props.maxTargets)
			select p.Position).ToList();
	}

	private void DoLightningStrike(IntVec3 targetCell, Map map)
	{
		if (map.weatherManager != null)
		{
			map.weatherManager.eventHandler.AddEvent(new WeatherEvent_LightningStrike(map, targetCell));
		}
		else
		{
			FleckMaker.ThrowLightningGlow(targetCell.ToVector3(), map, 3f);
		}
		GenExplosion.DoExplosion(targetCell, map, Props.empRadius, Props.damageType, parent, Props.damageAmount, 0f);
	}
}
