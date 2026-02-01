using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace GD3
{
	public class WorkGiver_AddDecks : WorkGiver_Scanner
	{
		public override PathEndMode PathEndMode => PathEndMode.Touch;

		public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
		{
			return pawn.Map.listerBuildings.allBuildingsColonistCombatTargets;
		}

		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			if (!pawn.CanReserve(t, 1, -1, null, forced))
			{
				return false;
			}
			if (GDUtility.FindBestThing(pawn, GDDefOf.PrecisionDeck) == null)
			{
				return false;
			}
			if (!(t is Building_Turret key))
            {
				return false;
            }
			CompDeckReinforce compDeckReinforce = key.TryGetComp<CompDeckReinforce>();
			if (compDeckReinforce == null || !compDeckReinforce.shouldBeNoticed)
            {
				return false;
            }
			if (compDeckReinforce.IsFull)
            {
				return false;
            }
			int num = compDeckReinforce.CountToFullyRefuel;
			if (RefuelWorkGiverUtility.FindEnoughReservableThings(pawn, t.Position, new IntRange(num, num), (Thing th) => th.def == GDDefOf.PrecisionDeck) == null)
			{
				return false;
			}
			return true;
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			Job job = JobMaker.MakeJob(GDDefOf.GD_HaulToTurret, t, GDUtility.FindBestThing(pawn, GDDefOf.PrecisionDeck));
			return job;
		}
	}

}
