using System;
using Verse;

namespace CharacterEditor;

internal static class AgeTool
{
	internal const string CO_AGECHRONOLOGICAL = "birthAbsTicksInt";

	internal static void SetChronoAgeTicks(this Pawn p, long val)
	{
		if (p.HasAgeTracker())
		{
			p.ageTracker.SetMemberValue("birthAbsTicksInt", val);
		}
	}

	internal static long GetAgeTicks(this Pawn p)
	{
		return p.HasAgeTracker() ? p.ageTracker.AgeBiologicalTicks : 0;
	}

	internal static long GetChronoAgeTicks(this Pawn p)
	{
		return p.HasAgeTracker() ? p.ageTracker.BirthAbsTicks : 0;
	}

	internal static void SetAgeTicks(this Pawn p, long ageTicks)
	{
		if (!p.HasAgeTracker())
		{
			return;
		}
		try
		{
			p.ageTracker.AgeBiologicalTicks = ageTicks;
			p.Recalculate_WorkTypes();
			CompatibilityTool.UpdateLifestage(p);
		}
		catch (Exception e)
		{
			MessageTool.DebugException(e);
		}
	}

	internal static void SetAge(this Pawn p, int age)
	{
		if (p.HasAgeTracker())
		{
			p.ageTracker.AgeBiologicalTicks = (long)age * 3600000L;
			p.Recalculate_WorkTypes();
			CompatibilityTool.UpdateLifestage(p);
		}
	}

	internal static void Recalculate_WorkTypes(this Pawn p)
	{
		p.workSettings?.Notify_DisabledWorkTypesChanged();
		p.Notify_DisabledWorkTypesChanged();
		p.RaceProps.ResolveReferencesSpecial();
		p.def.ResolveReferences();
	}

	internal static void SetChronoAge(this Pawn p, int age)
	{
		if (p.HasAgeTracker())
		{
			p.ageTracker.SetMemberValue("birthAbsTicksInt", GenTicks.TicksAbs - (long)age * 3600000L);
		}
	}

	internal static void SetChronoAgeDay(this Pawn p, int age, int ticks)
	{
		if (p.HasAgeTracker())
		{
			p.ageTracker.SetMemberValue("birthAbsTicksInt", GenTicks.TicksAbs - (long)age * 3600000L - (long)ticks * 60000L);
		}
	}
}
