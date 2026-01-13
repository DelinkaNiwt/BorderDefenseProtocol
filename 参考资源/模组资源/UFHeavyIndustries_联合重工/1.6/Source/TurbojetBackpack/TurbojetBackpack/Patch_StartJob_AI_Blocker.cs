using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace TurbojetBackpack;

[HarmonyPatch(typeof(Pawn_JobTracker), "StartJob")]
public static class Patch_StartJob_AI_Blocker
{
	public static bool Prefix(Job newJob, ThinkNode jobGiver, Pawn ___pawn)
	{
		if (___pawn == null || ___pawn.Map == null || newJob == null)
		{
			return true;
		}
		if (newJob.playerForced)
		{
			return true;
		}
		if (TurbojetGlobal.IsFlightActive(___pawn) && !___pawn.Position.WalkableBy(___pawn.Map, ___pawn) && (newJob.def == JobDefOf.Goto || newJob.def == JobDefOf.GotoWander))
		{
			newJob.def = JobDefOf.Wait_Combat;
			newJob.expiryInterval = 60;
			newJob.targetA = LocalTargetInfo.Invalid;
			return true;
		}
		return true;
	}
}
