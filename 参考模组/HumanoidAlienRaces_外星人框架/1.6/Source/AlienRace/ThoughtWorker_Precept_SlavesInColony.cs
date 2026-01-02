using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;

namespace AlienRace;

public class ThoughtWorker_Precept_SlavesInColony : ThoughtWorker_Precept
{
	protected override ThoughtState ShouldHaveThought(Pawn p)
	{
		Lord lord = p.GetLord();
		if (lord != null && lord.ownedPawns.Any((Pawn c) => Utilities.DifferentRace(p.def, c.def) && c.IsSlave))
		{
			return true;
		}
		Caravan car = p.GetCaravan();
		if (car != null && car.PawnsListForReading.Any((Pawn c) => Utilities.DifferentRace(p.def, c.def) && c.IsSlave))
		{
			return true;
		}
		Map map = p.MapHeld;
		if (map != null)
		{
			Faction fac = p.Faction;
			if (fac != null)
			{
				if (map.mapPawns.SpawnedPawnsInFaction(fac).Any((Pawn c) => Utilities.DifferentRace(p.def, c.def) && c.IsSlave))
				{
					return true;
				}
			}
			else if (map.mapPawns.AllPawnsSpawned.Any((Pawn c) => Utilities.DifferentRace(p.def, c.def) && !p.HostileTo(c) && c.IsSlave))
			{
				return true;
			}
		}
		return false;
	}
}
