using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;

namespace AlienRace;

[UsedImplicitly]
public class ThoughtWorker_Precept_AlienRaces : ThoughtWorker_Precept
{
	protected override ThoughtState ShouldHaveThought(Pawn p)
	{
		Lord lord = p.GetLord();
		if (lord != null && lord.ownedPawns.Any((Pawn c) => Utilities.DifferentRace(p.def, c.def)))
		{
			return true;
		}
		Caravan car = p.GetCaravan();
		if (car != null && car.PawnsListForReading.Any((Pawn c) => Utilities.DifferentRace(p.def, c.def)))
		{
			return true;
		}
		Map map = p.MapHeld;
		if (map != null)
		{
			Faction fac = p.Faction;
			if (fac != null)
			{
				if (map.mapPawns.SpawnedPawnsInFaction(fac).Any((Pawn c) => Utilities.DifferentRace(p.def, c.def)))
				{
					return true;
				}
			}
			else if (map.mapPawns.AllPawnsSpawned.Any((Pawn c) => Utilities.DifferentRace(p.def, c.def) && !p.HostileTo(c)))
			{
				return true;
			}
		}
		return false;
	}
}
