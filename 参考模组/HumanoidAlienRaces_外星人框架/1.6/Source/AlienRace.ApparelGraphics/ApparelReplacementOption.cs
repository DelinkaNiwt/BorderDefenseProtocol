using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace AlienRace.ApparelGraphics;

public class ApparelReplacementOption
{
	public AlienPartGenerator.ExtendedGraphicTop wornGraphicPath = new AlienPartGenerator.ExtendedGraphicTop
	{
		path = string.Empty
	};

	public List<AlienPartGenerator.ExtendedGraphicTop> wornGraphicPaths = new List<AlienPartGenerator.ExtendedGraphicTop>();

	public List<string> apparelTags;

	public List<BodyPartGroupDef> bodyPartGroups = new List<BodyPartGroupDef>();

	public List<ApparelLayerDef> layers = new List<ApparelLayerDef>();

	public AlienPartGenerator.ExtendedGraphicTop GetGraphics(ThingDef apparelDef)
	{
		if (wornGraphicPaths.NullOrEmpty())
		{
			return wornGraphicPath;
		}
		return wornGraphicPaths[apparelDef.GetHashCode() % wornGraphicPaths.Count];
	}

	public bool IsSuitableReplacementFor(ThingDef apparelDef)
	{
		if (apparelDef?.apparel == null)
		{
			return false;
		}
		ApparelProperties props = apparelDef.apparel;
		if ((apparelTags.NullOrEmpty() || (!props.tags.NullOrEmpty() && props.tags.Intersect(apparelTags).Any())) && (bodyPartGroups.NullOrEmpty() || (!props.bodyPartGroups.NullOrEmpty() && props.bodyPartGroups.Intersect(bodyPartGroups).Any())))
		{
			if (!layers.NullOrEmpty())
			{
				if (!props.layers.NullOrEmpty())
				{
					return props.layers.Intersect(layers).Any();
				}
				return false;
			}
			return true;
		}
		return false;
	}
}
