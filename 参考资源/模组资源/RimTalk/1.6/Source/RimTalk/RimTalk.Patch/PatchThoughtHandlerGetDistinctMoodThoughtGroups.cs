using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RimTalk.Patch;

[HarmonyPatch(typeof(ThoughtHandler), "GetDistinctMoodThoughtGroups")]
public static class PatchThoughtHandlerGetDistinctMoodThoughtGroups
{
	private static readonly Dictionary<Pawn, HashSet<string>> LastSituationalThoughts = new Dictionary<Pawn, HashSet<string>>();

	private static void Postfix(ThoughtHandler __instance, List<Thought> outThoughts)
	{
		if (Current.ProgramState != ProgramState.Playing || __instance.pawn == null || !__instance.pawn.Spawned)
		{
			return;
		}
		HashSet<string> currentThoughts = new HashSet<string>();
		foreach (Thought thought in outThoughts)
		{
			if (thought is Thought_Situational)
			{
				currentThoughts.Add(thought.def.defName);
			}
		}
		if (!LastSituationalThoughts.TryGetValue(__instance.pawn, out var previousThoughts))
		{
			previousThoughts = new HashSet<string>();
			LastSituationalThoughts[__instance.pawn] = previousThoughts;
		}
		List<string> newThoughts = currentThoughts.Except(previousThoughts).ToList();
		foreach (string thoughtDefName in newThoughts)
		{
			Thought thought2 = outThoughts.FirstOrDefault((Thought t) => t.def.defName == thoughtDefName);
			if (thought2 == null)
			{
				continue;
			}
			try
			{
				if (!(thought2.MoodOffset() > 0f) || !__instance.pawn.InMentalState)
				{
					ThoughtTracker.TryMarkAsProcessed(__instance.pawn, thought2);
				}
			}
			catch
			{
			}
		}
		LastSituationalThoughts[__instance.pawn] = currentThoughts;
	}

	public static void Clear()
	{
		LastSituationalThoughts.Clear();
	}
}
