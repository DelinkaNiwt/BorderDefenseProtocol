using RimWorld;
using Verse;

namespace RimTalk.Source.Data;

public static class InteractionExtensions
{
	public static ThoughtDef? GetThoughtDef(this InteractionType type)
	{
		if (1 == 0)
		{
		}
		ThoughtDef result = type switch
		{
			InteractionType.Insult => DefDatabase<ThoughtDef>.GetNamed("RimTalk_Slighted"), 
			InteractionType.Slight => DefDatabase<ThoughtDef>.GetNamed("RimTalk_Slighted"), 
			InteractionType.Chat => DefDatabase<ThoughtDef>.GetNamed("RimTalk_Chitchat"), 
			InteractionType.Kind => DefDatabase<ThoughtDef>.GetNamed("RimTalk_KindWords"), 
			_ => null, 
		};
		if (1 == 0)
		{
		}
		return result;
	}
}
