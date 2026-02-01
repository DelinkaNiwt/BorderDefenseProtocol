using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RimTalk.Patch;

[HarmonyPatch(typeof(MemoryThoughtHandler), "TryGainMemory")]
[HarmonyPatch(new Type[]
{
	typeof(Thought_Memory),
	typeof(Pawn)
})]
public static class PatchMemoryThoughtHandlerTryGainMemory
{
	private static void Postfix(Thought_Memory newThought, Pawn otherPawn)
	{
		if (Current.ProgramState == ProgramState.Playing && newThought?.pawn != null && !(newThought is Thought_MemorySocial))
		{
			float moodImpact;
			try
			{
				moodImpact = newThought.MoodOffset();
			}
			catch (Exception)
			{
				return;
			}
			if (!(Math.Abs(moodImpact) < 3f) && (!(moodImpact > 0f) || !newThought.pawn.InMentalState))
			{
				ThoughtTracker.TryMarkAsProcessed(newThought.pawn, newThought);
			}
		}
	}
}
