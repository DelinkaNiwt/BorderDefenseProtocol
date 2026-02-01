using System.Collections.Generic;
using LudeonTK;
using UnityEngine;

namespace VanillaPsycastsExpanded.Nightstalker;

public static class DecoyOverlayUtility
{
	[TweakValue("00", 0f, 1f)]
	public static float ColorR = 0f;

	[TweakValue("00", 0f, 1f)]
	public static float ColorG = 0f;

	[TweakValue("00", 0f, 1f)]
	public static float ColorB = 0f;

	[TweakValue("00", 0f, 1f)]
	public static float ColorA = 1f;

	private static readonly Dictionary<Material, Material> Materials = new Dictionary<Material, Material>();

	public static bool DrawOverlay;

	public static Color OverlayColor => new Color(ColorR, ColorG, ColorB, ColorA);

	public static Material GetDuplicateMat(Material baseMat)
	{
		if (!Materials.TryGetValue(baseMat, out var value))
		{
			value = MaterialAllocator.Create(baseMat);
			value.color = OverlayColor;
			Materials[baseMat] = value;
		}
		return value;
	}
}
