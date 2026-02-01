using Verse;

namespace RimTalk.Util;

public static class Describer
{
	public static string Wealth(float wealthTotal)
	{
		if (1 == 0)
		{
		}
		string result = ((wealthTotal < 50000f) ? "impecunious" : ((wealthTotal < 100000f) ? "needy" : ((wealthTotal < 200000f) ? "just rid of starving" : ((wealthTotal < 300000f) ? "moderately prosperous" : ((wealthTotal < 400000f) ? "rich" : ((wealthTotal < 600000f) ? "luxurious" : ((wealthTotal < 1000000f) ? "extravagant" : ((wealthTotal < 1500000f) ? "treasures fill the home" : ((!(wealthTotal < 2000000f)) ? "richest in the galaxy" : "as rich as glitter world")))))))));
		if (1 == 0)
		{
		}
		return result;
	}

	public static string Beauty(float beauty)
	{
		if (1 == 0)
		{
		}
		string result = ((beauty > 100f) ? "wondrously" : ((beauty > 20f) ? "impressive" : ((beauty > 10f) ? "beautiful" : ((beauty > 5f) ? "decent" : ((beauty > -1f) ? "general" : ((beauty > -5f) ? "awful" : ((!(beauty > -20f)) ? "disgusting" : "very awful")))))));
		if (1 == 0)
		{
		}
		return result;
	}

	public static string Cleanliness(float cleanliness)
	{
		if (1 == 0)
		{
		}
		string result = ((cleanliness > 1.5f) ? "spotless" : ((cleanliness > 0.5f) ? "clean" : ((cleanliness > -0.5f) ? "neat" : ((cleanliness > -1.5f) ? "a bit dirty" : ((cleanliness > -2.5f) ? "dirty" : ((!(cleanliness > -5f)) ? "foul" : "very dirty"))))));
		if (1 == 0)
		{
		}
		return result;
	}

	public static string Resistance(float value)
	{
		if (value <= 0f)
		{
			return "Completely broken, ready to join";
		}
		if (value < 2f)
		{
			return "Barely resisting, close to giving in";
		}
		if (value < 6f)
		{
			return "Weakened, but still cautious";
		}
		if (value < 12f)
		{
			return "Strong-willed, requires effort";
		}
		return "Extremely defiant, will take a long time";
	}

	public static string Will(float value)
	{
		if (value <= 0f)
		{
			return "No will left, ready for slavery";
		}
		if (value < 2f)
		{
			return "Weak-willed, easy to enslave";
		}
		if (value < 6f)
		{
			return "Moderate will, may resist a little";
		}
		if (value < 12f)
		{
			return "Strong will, difficult to enslave";
		}
		return "Unyielding, very hard to enslave";
	}

	public static string Suppression(float value)
	{
		if (value < 20f)
		{
			return "Openly rebellious, likely to resist or escape";
		}
		if (value < 50f)
		{
			return "Unstable, may push boundaries";
		}
		if (value < 80f)
		{
			return "Generally obedient, but watchful";
		}
		return "Completely cowed, unlikely to resist";
	}

	public static string GetLabelShort(this Gender gender)
	{
		if (1 == 0)
		{
		}
		string result = gender switch
		{
			Gender.Male => "M", 
			Gender.Female => "F", 
			_ => "", 
		};
		if (1 == 0)
		{
		}
		return result;
	}
}
