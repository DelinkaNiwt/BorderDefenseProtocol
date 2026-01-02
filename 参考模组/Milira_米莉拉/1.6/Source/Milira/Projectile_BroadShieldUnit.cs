using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Milira;

public class Projectile_BroadShieldUnit : Projectile
{
	protected override void Impact(Thing hitThing, bool blockedByShield = false)
	{
		Map map = base.Map;
		base.Impact(hitThing);
		Thing thing = ThingMaker.MakeThing(MiliraDefOf.Milian_BroadShieldUnit);
		thing.stackCount = 1;
		GenSpawn.Spawn(thing, base.Position, map);
		thing.SetFaction(launcher.Faction);
		if (def.projectile.explosionEffect != null)
		{
			Effecter effecter = def.projectile.explosionEffect.Spawn();
			effecter.Trigger(new TargetInfo(base.Position, map), new TargetInfo(base.Position, map));
			effecter.Cleanup();
		}
		List<Pawn> list = new List<Pawn>();
		IEnumerable<Pawn> enumerable = map.mapPawns.AllPawnsSpawned.Where((Pawn p) => p.Position.InHorDistOf(base.Position, 4.9f));
		foreach (Pawn item in enumerable)
		{
			if (item.Faction != launcher.Faction && !item.Downed && !item.Dead)
			{
				IntVec3 position = TargetPosition(launcher.Position, item, 6f);
				item.Position = position;
				item.pather.StopDead();
				item.jobs.StopAll();
			}
		}
	}

	public static IntVec3 TargetPosition(IntVec3 directionRef, Pawn pawn2, float distance)
	{
		IntVec3 position = pawn2.Position;
		IntVec3 result = position;
		Vector3 normalized = (position - directionRef).ToVector3().normalized;
		Map map = pawn2.Map;
		for (int i = 0; (float)i < distance; i++)
		{
			Vector3 vect = i * normalized;
			IntVec3 intVec = position + vect.ToIntVec3();
			if (!ValidKnockBackTarget(map, intVec))
			{
				break;
			}
			result = intVec;
		}
		return result;
	}

	public static bool ValidKnockBackTarget(Map map, IntVec3 cell)
	{
		if (!cell.IsValid || !cell.InBounds(map))
		{
			return false;
		}
		if (cell.Impassable(map) || !cell.Walkable(map) || cell.Fogged(map))
		{
			return false;
		}
		Building edifice = cell.GetEdifice(map);
		if (edifice != null && edifice is Building_Door { Open: false })
		{
			return false;
		}
		return true;
	}
}
