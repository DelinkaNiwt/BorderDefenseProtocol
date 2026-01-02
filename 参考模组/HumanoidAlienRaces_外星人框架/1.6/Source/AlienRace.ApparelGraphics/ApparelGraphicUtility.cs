using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AlienRace.ApparelGraphics;

public static class ApparelGraphicUtility
{
	public static Dictionary<(ThingDef, ThingDef_AlienRace, BodyTypeDef), AlienPartGenerator.ExtendedGraphicTop> apparelCache = new Dictionary<(ThingDef, ThingDef_AlienRace, BodyTypeDef), AlienPartGenerator.ExtendedGraphicTop>();

	public static Graphic GetGraphic(string path, Shader shader, Vector2 drawSize, Color color, Apparel apparel, BodyTypeDef bodyType)
	{
		return GraphicDatabase.Get<Graphic_Multi>(GetPath(path, apparel, bodyType) ?? path, shader, drawSize, color, apparel.DrawColorTwo);
	}

	public static string GetPath(string path, Apparel apparel, BodyTypeDef bodyType)
	{
		Pawn wearer = apparel.Wearer;
		ThingDef_AlienRace alienRace = wearer?.def as ThingDef_AlienRace;
		if (alienRace != null || (alienRace = AlienPartGenerator.ExtendedGraphicTop.drawOverrideDummy?.race as ThingDef_AlienRace) != null)
		{
			int index = 0;
			int savedIndex = wearer?.HashOffset() ?? apparel.HashOffset();
			if (apparelCache.TryGetValue((apparel.def, alienRace, bodyType), out var cachedGraphic))
			{
				return cachedGraphic.GetPath(wearer, ref index, savedIndex);
			}
			ApparelGraphicsOverrides overrides = alienRace.alienRace.graphicPaths.apparel;
			AlienPartGenerator.ExtendedGraphicTop overrideEGraphic;
			if ((overrideEGraphic = overrides.GetOverride(apparel)) != null)
			{
				return overrideEGraphic.GetPath(wearer, ref index, savedIndex);
			}
			overrideEGraphic = overrides.pathPrefix;
			string overridePath;
			if (overrideEGraphic != null)
			{
				overridePath = overrideEGraphic.GetPath(wearer, ref index, savedIndex);
				if (!overridePath.NullOrEmpty() && ValidTexturesExist(overridePath += path))
				{
					return overridePath;
				}
			}
			if (ValidTexturesExist(path))
			{
				return path;
			}
			if ((overrideEGraphic = overrides.GetFallbackPath(apparel)) != null)
			{
				return overrideEGraphic.GetPath(wearer, ref index, savedIndex);
			}
			if (bodyType != null && path.EndsWith(overridePath = "_" + bodyType.defName) && overrides.TryGetBodyTypeFallback(wearer?.gender ?? AlienPartGenerator.ExtendedGraphicTop.drawOverrideDummy?.gender ?? Gender.None, out var overrideBodyType))
			{
				return ReplaceBodyType(path, overridePath, overrideBodyType);
			}
		}
		return null;
	}

	public static bool ValidTexturesExist(string path)
	{
		Texture2D southTexture = ContentFinder<Texture2D>.Get(path + "_south", reportFailure: false);
		if (southTexture == null)
		{
			return ContentFinder<Texture2D>.Get(path, reportFailure: false) != null;
		}
		return true;
	}

	public static string ReplaceBodyType(string path, string oldToken, BodyTypeDef newBodyType)
	{
		int index = path.LastIndexOf(oldToken, StringComparison.Ordinal);
		return path.Remove(index) + "_" + newBodyType.defName;
	}
}
