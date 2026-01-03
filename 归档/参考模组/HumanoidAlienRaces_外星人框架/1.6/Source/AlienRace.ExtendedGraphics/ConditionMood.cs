using System;

namespace AlienRace.ExtendedGraphics;

public class ConditionMood : Condition
{
	[Flags]
	public enum MoodState : byte
	{
		ABOUT_TO_BREAK = 1,
		ON_EDGE = 2,
		STRESSED = 4,
		BAD = 7,
		NEUTRAL = 8,
		CONTENT = 0x10,
		HAPPY = 0x20
	}

	public new const string XmlNameParseKey = "Mood";

	public MoodState state = MoodState.CONTENT;

	public override bool Satisfied(ExtendedGraphicsPawnWrapper pawn, ref ResolveData data)
	{
		float need = pawn.GetNeed(AlienDefOf.Mood, percentage: false);
		return state.HasFlag(GetMood(pawn, need));
	}

	private static MoodState GetMood(ExtendedGraphicsPawnWrapper pawn, float need)
	{
		float breakThresholdExtreme = pawn.WrappedPawn.mindState.mentalBreaker.BreakThresholdExtreme;
		if (need < breakThresholdExtreme)
		{
			return MoodState.ABOUT_TO_BREAK;
		}
		if (need < breakThresholdExtreme + 0.05f)
		{
			return MoodState.ON_EDGE;
		}
		if (need < pawn.WrappedPawn.mindState.mentalBreaker.BreakThresholdMinor)
		{
			return MoodState.STRESSED;
		}
		if (!(need < 0.65f))
		{
			if (need < 0.9f)
			{
				return MoodState.CONTENT;
			}
			return MoodState.HAPPY;
		}
		return MoodState.NEUTRAL;
	}
}
