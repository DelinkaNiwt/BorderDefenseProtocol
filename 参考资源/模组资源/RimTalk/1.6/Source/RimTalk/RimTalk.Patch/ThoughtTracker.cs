using System.Collections.Generic;
using RimTalk.Data;
using RimTalk.Source.Data;
using RimWorld;
using Verse;

namespace RimTalk.Patch;

public static class ThoughtTracker
{
	public static void TryMarkAsProcessed(Pawn pawn, Thought thought)
	{
		if (pawn != null && thought != null && thought.def != null)
		{
			Hediff_Persona hediff = Hediff_Persona.GetOrAddNew(pawn);
			if (hediff != null && hediff.TryMarkAsSpoken(thought))
			{
				Cache.Get(pawn)?.AddTalkRequest(GetThoughtLabel(thought), pawn, TalkType.Thought);
			}
		}
	}

	public static string GetThoughtLabel(Thought thought)
	{
		if (thought == null)
		{
			return null;
		}
		float offset;
		try
		{
			offset = thought.MoodOffset();
		}
		catch
		{
			return null;
		}
		if (offset > 0f)
		{
			return "new good feeling: " + thought.LabelCap;
		}
		if (offset < 0f)
		{
			return "new bad feeling: " + thought.LabelCap;
		}
		return "new feeling: " + thought.LabelCap;
	}

	public static bool IsThoughtStillActive(Pawn pawn, string thoughtLabel)
	{
		if (pawn?.needs?.mood?.thoughts == null || string.IsNullOrEmpty(thoughtLabel))
		{
			return false;
		}
		List<Thought_Memory> memoryThoughts = pawn.needs.mood.thoughts.memories?.Memories;
		if (memoryThoughts != null && memoryThoughts.Any((Thought_Memory m) => m != null && !string.IsNullOrEmpty(m.LabelCap) && thoughtLabel.Contains(m.LabelCap)))
		{
			return true;
		}
		List<Thought> allThoughts = new List<Thought>();
		pawn.needs.mood.thoughts.GetAllMoodThoughts(allThoughts);
		return allThoughts.Any((Thought t) => t != null && !string.IsNullOrEmpty(t.LabelCap) && thoughtLabel.Contains(t.LabelCap));
	}
}
