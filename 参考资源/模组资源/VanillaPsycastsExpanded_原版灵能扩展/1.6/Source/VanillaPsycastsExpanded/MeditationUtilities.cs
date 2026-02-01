using LudeonTK;
using RimWorld;
using Verse;

namespace VanillaPsycastsExpanded;

public static class MeditationUtilities
{
	[DebugAction("Pawns", "Check Meditation Focus Strength", true, false, false, false, false, 0, false, actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
	public static void CheckStrength(Pawn pawn)
	{
		float statValue = pawn.GetStatValue(StatDefOf.MeditationFocusGain);
		Log.Message($"Value: {statValue}, Explanation:\n{StatDefOf.MeditationFocusGain.Worker.GetExplanationFull(StatRequest.For(pawn), ToStringNumberSense.Absolute, statValue)}");
		LocalTargetInfo localTargetInfo = MeditationUtility.BestFocusAt(pawn.Position, pawn);
		if (localTargetInfo.HasThing)
		{
			statValue = localTargetInfo.Thing.GetStatValueForPawn(StatDefOf.MeditationFocusStrength, pawn);
			Log.Message($"Value: {statValue}, Explanation:\n{StatDefOf.MeditationFocusStrength.Worker.GetExplanationFull(StatRequest.For(localTargetInfo.Thing, pawn), ToStringNumberSense.Absolute, statValue)}");
		}
	}

	public static bool CanUnlock(this MeditationFocusDef focus, Pawn pawn, out string reason)
	{
		if (focus == VPE_DefOf.Dignified && (pawn.royalty == null || !pawn.royalty.AllTitlesForReading.Any() || !pawn.royalty.CanUpdateTitleOfAnyFaction(out var _)))
		{
			reason = "VPE.LockedTitle".Translate();
			return false;
		}
		MeditationFocusExtension modExtension = focus.GetModExtension<MeditationFocusExtension>();
		if (modExtension != null && !modExtension.canBeUnlocked)
		{
			reason = "VPE.LockedLocked".Translate();
			return false;
		}
		reason = null;
		return true;
	}
}
