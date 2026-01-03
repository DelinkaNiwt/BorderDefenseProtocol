using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Milira;

[HarmonyPatch(typeof(Pawn_FlightTracker))]
[HarmonyPatch("Notify_JobStarted")]
public static class MilianPatch_Pawn_FlightTracker_Notify_JobStarted
{
	[HarmonyPrefix]
	public static bool Prefix(Job job, Pawn_FlightTracker __instance, Pawn ___pawn)
	{
		if (___pawn?.RaceProps?.body?.defName == "Milira_Body")
		{
			CompFlightControl compFlightControl = ___pawn.TryGetComp<CompFlightControl>();
			if (__instance.CanEverFly && compFlightControl.CanFly && (___pawn.Drafted || ___pawn.mindState.enemyTarget != null))
			{
				__instance.StartFlying();
				job.flying = true;
				Hediff hediff = HediffMaker.MakeHediff(MiliraDefOf.Milira_InFlight, ___pawn);
				___pawn.health.AddHediff(hediff);
				return false;
			}
			job.flying = false;
			if (__instance.Flying)
			{
				__instance.ForceLand();
			}
			return false;
		}
		return true;
	}
}
