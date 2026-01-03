using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace AncotLibrary;

[HarmonyPatch(typeof(TransporterUtility))]
[HarmonyPatch("MakeLordsAsAppropriate")]
public static class AncotPatch_MakeTranportsLord_Drone
{
	[HarmonyPostfix]
	public static void Postfix(List<Pawn> pawns, List<CompTransporter> transporters, Map map)
	{
		int groupID = transporters[0].groupID;
		Lord lord = null;
		IEnumerable<Pawn> enumerable = pawns.Where((Pawn x) => (x.TryGetComp<CompDrone>() != null || x.IsColonist || x.IsColonyMechPlayerControlled) && !x.Downed && x.Spawned);
		if (enumerable.Any())
		{
			lord = map.lordManager.lords.Find((Lord x) => x.LordJob is LordJob_LoadAndEnterTransporters lordJob_LoadAndEnterTransporters2 && lordJob_LoadAndEnterTransporters2.transportersGroup == groupID);
			if (lord == null)
			{
				lord = LordMaker.MakeNewLord(Faction.OfPlayer, new LordJob_LoadAndEnterTransporters(groupID), map);
			}
			foreach (Pawn item in enumerable)
			{
				if (!lord.ownedPawns.Contains(item))
				{
					item.GetLord()?.Notify_PawnLost(item, PawnLostCondition.ForcedToJoinOtherLord);
					lord.AddPawn(item);
					item.jobs.EndCurrentJob(JobCondition.InterruptForced);
				}
			}
			for (int num = lord.ownedPawns.Count - 1; num >= 0; num--)
			{
				if (!enumerable.Contains(lord.ownedPawns[num]))
				{
					lord.Notify_PawnLost(lord.ownedPawns[num], PawnLostCondition.NoLongerEnteringTransportPods);
				}
			}
		}
		for (int num2 = map.lordManager.lords.Count - 1; num2 >= 0; num2--)
		{
			if (map.lordManager.lords[num2].LordJob is LordJob_LoadAndEnterTransporters lordJob_LoadAndEnterTransporters && lordJob_LoadAndEnterTransporters.transportersGroup == groupID && map.lordManager.lords[num2] != lord)
			{
				map.lordManager.RemoveLord(map.lordManager.lords[num2]);
			}
		}
	}
}
