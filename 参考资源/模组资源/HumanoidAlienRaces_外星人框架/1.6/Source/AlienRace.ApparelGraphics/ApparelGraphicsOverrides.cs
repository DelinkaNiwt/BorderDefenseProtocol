using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AlienRace.ApparelGraphics;

public class ApparelGraphicsOverrides
{
	public AlienPartGenerator.ExtendedGraphicTop pathPrefix = new AlienPartGenerator.ExtendedGraphicTop
	{
		path = string.Empty
	};

	public Dictionary<ThingDef, AlienPartGenerator.ExtendedGraphicTop> individualPaths = new Dictionary<ThingDef, AlienPartGenerator.ExtendedGraphicTop>();

	public List<ApparelReplacementOption> overrides = new List<ApparelReplacementOption>();

	public List<ApparelReplacementOption> fallbacks = new List<ApparelReplacementOption>();

	public BodyTypeDef bodyTypeFallback;

	public BodyTypeDef femaleBodyTypeFallback;

	public AlienPartGenerator.ExtendedGraphicTop GetOverride(Apparel apparel)
	{
		if (apparel?.def == null)
		{
			return null;
		}
		if (individualPaths.NullOrEmpty() || !individualPaths.TryGetValue(apparel.def, out var overridePath))
		{
			if (overrides.NullOrEmpty())
			{
				return null;
			}
			return overrides.FirstOrDefault((ApparelReplacementOption apo) => apo.IsSuitableReplacementFor(apparel.def))?.GetGraphics(apparel.def);
		}
		return overridePath;
	}

	public AlienPartGenerator.ExtendedGraphicTop GetFallbackPath(Apparel apparel)
	{
		if (apparel?.def == null)
		{
			return null;
		}
		if (fallbacks.NullOrEmpty())
		{
			return null;
		}
		return fallbacks.FirstOrDefault((ApparelReplacementOption option) => option.IsSuitableReplacementFor(apparel.def))?.GetGraphics(apparel.def);
	}

	public bool TryGetBodyTypeFallback(Gender? gender, out BodyTypeDef def)
	{
		def = null;
		if (!gender.HasValue)
		{
			return false;
		}
		def = ((gender == Gender.Female && femaleBodyTypeFallback != null) ? femaleBodyTypeFallback : bodyTypeFallback);
		return def != null;
	}
}
