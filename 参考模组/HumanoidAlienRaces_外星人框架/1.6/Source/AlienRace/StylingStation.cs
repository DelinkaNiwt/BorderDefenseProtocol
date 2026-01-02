using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AlienRace;

[StaticConstructorOnStartup]
public static class StylingStation
{
	private enum MainTab
	{
		CHARACTER,
		RACE
	}

	private enum RaceTab
	{
		COLORS,
		BODY_ADDONS
	}

	private static readonly Texture2D ChainTex = ContentFinder<Texture2D>.Get("AlienRace/UI/LinkChain");

	private static readonly Texture2D ClearTex = ContentFinder<Texture2D>.Get("AlienRace/UI/ClearButton");

	private static readonly Texture2D ChainVanillaTex = ContentFinder<Texture2D>.Get("AlienRace/UI/LinkVanilla");

	private static readonly List<TabRecord> mainTabs = new List<TabRecord>();

	private static readonly List<TabRecord> raceTabs = new List<TabRecord>();

	private static MainTab curMainTab;

	private static RaceTab curRaceTab = RaceTab.BODY_ADDONS;

	private static Dialog_StylingStation instance;

	private static Pawn pawn;

	private static AlienPartGenerator.AlienComp alienComp;

	private static ThingDef_AlienRace alienRaceDef;

	private static List<int> addonVariants;

	private static List<AlienPartGenerator.ExposableValueTuple<Color?, Color?>> addonColors;

	private static Dictionary<string, AlienPartGenerator.ExposableValueTuple<Color, Color>> colorChannels;

	private static readonly Dictionary<AlienPartGenerator.ColorChannelGenerator, Dictionary<bool, List<Color>>> availableColorsCache = new Dictionary<AlienPartGenerator.ColorChannelGenerator, Dictionary<bool, List<Color>>>();

	private static int selectedIndexAddons = -1;

	private static Vector2 addonsScrollPos;

	private static Vector2 variantsScrollPos;

	private static bool editingFirstColor = true;

	private static Vector2 colorsScrollPos;

	private static int selectedIndexChannels = -1;

	private static Vector2 channelsScrollPos;

	private static float channelColorViewRectHeight;

	public static void ConstructorPostfix(Pawn pawn)
	{
		StylingStation.pawn = pawn;
		alienComp = pawn.TryGetComp<AlienPartGenerator.AlienComp>();
		alienRaceDef = pawn.def as ThingDef_AlienRace;
		addonVariants = alienComp.addonVariants.ToList();
		addonColors = alienComp.addonColors.ToList();
		colorChannels = new Dictionary<string, AlienPartGenerator.ExposableValueTuple<Color, Color>>(alienComp.ColorChannels);
		availableColorsCache.Clear();
		List<string> list = colorChannels.Keys.ToList();
		foreach (string key in list)
		{
			colorChannels[key] = (AlienPartGenerator.ExposableValueTuple<Color, Color>)colorChannels[key].Clone();
		}
	}

	public static IEnumerable<CodeInstruction> DoWindowContentsTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
	{
		return instructions.MethodReplacer(AccessTools.Method(typeof(Dialog_StylingStation), "DrawTabs"), AccessTools.Method(typeof(StylingStation), "DoRaceAndCharacterTabs"));
	}

	public static void DoRaceAndCharacterTabs(Dialog_StylingStation gotInstance, Rect inRect)
	{
		instance = gotInstance;
		if (alienRaceDef == null)
		{
			CachedData.drawTabs(instance, inRect);
			return;
		}
		mainTabs.Clear();
		mainTabs.Add(new TabRecord("HAR.CharacterFeatures".Translate(), delegate
		{
			curMainTab = MainTab.CHARACTER;
		}, curMainTab == MainTab.CHARACTER));
		mainTabs.Add(new TabRecord("HAR.RaceFeatures".Translate(), delegate
		{
			curMainTab = MainTab.RACE;
		}, curMainTab == MainTab.RACE));
		Widgets.DrawMenuSection(inRect);
		TabDrawer.DrawTabs(inRect, mainTabs);
		inRect.yMin += 40f;
		switch (curMainTab)
		{
		case MainTab.CHARACTER:
			CachedData.drawTabs(instance, inRect);
			break;
		case MainTab.RACE:
			DoRaceTabs(inRect);
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
	}

	public static List<Color> AvailableColors(AlienPartGenerator.BodyAddon ba, bool first = true)
	{
		AlienPartGenerator.ColorChannelGenerator channelGenerator = alienRaceDef.alienRace.generalSettings?.alienPartGenerator.colorChannels?.Find((AlienPartGenerator.ColorChannelGenerator ccg) => ccg.name == ba.ColorChannel);
		if (channelGenerator == null)
		{
			return new List<Color>();
		}
		return AvailableColors(channelGenerator, first);
	}

	private static List<Color> AvailableColors(AlienPartGenerator.ColorChannelGenerator channelGenerator, bool first)
	{
		if (availableColorsCache.TryGetValue(channelGenerator, out var colorEntry) && colorEntry.TryGetValue(first, out var colors))
		{
			return colors;
		}
		List<Color> availableColors = new List<Color>();
		foreach (AlienPartGenerator.ColorChannelGeneratorCategory entry in channelGenerator.entries)
		{
			ColorGenerator cg = (first ? entry.first : entry.second);
			availableColors.AddRange(AvailableColors(cg));
		}
		if (!availableColorsCache.ContainsKey(channelGenerator))
		{
			availableColorsCache.Add(channelGenerator, new Dictionary<bool, List<Color>>());
		}
		availableColorsCache[channelGenerator].Add(first, availableColors);
		return availableColors;
	}

	private static List<Color> AvailableColors(ColorGenerator colorGenerator)
	{
		List<Color> availableColors = new List<Color>();
		if (!(colorGenerator is IAlienChannelColorGenerator accg))
		{
			if (!(colorGenerator is ColorGenerator_CustomAlienChannel cgCustomAlien))
			{
				if (!(colorGenerator is ColorGenerator_SkinColorMelanin cgMelanin))
				{
					if (!(colorGenerator is ColorGenerator_Options cgOptions))
					{
						if (colorGenerator is ColorGenerator_Single || colorGenerator is ColorGenerator_White)
						{
							availableColors.Add(colorGenerator.NewRandomizedColor());
						}
					}
					else
					{
						foreach (ColorOption co in cgOptions.options)
						{
							if (co.only.a >= 0f)
							{
								availableColors.Add(co.only);
								continue;
							}
							List<Color> colorOptions = new List<Color>();
							Color diff = co.max - co.min;
							float redStep = Mathf.Max(0.001f, diff.r / 4f);
							float greenStep = Mathf.Max(0.001f, diff.g / 4f);
							float blueStep = Mathf.Max(0.001f, diff.b / 4f);
							float alphaStep = Mathf.Max(0.001f, diff.a / 2f);
							for (float r = co.min.r; r <= co.max.r; r += redStep)
							{
								for (float g = co.min.g; g <= co.max.g; g += greenStep)
								{
									for (float b = co.min.b; b <= co.max.b; b += blueStep)
									{
										for (float a = co.min.a; a <= co.max.a; a += alphaStep)
										{
											colorOptions.Add(new Color(r, g, b, a));
										}
									}
								}
							}
							availableColors.AddRange(colorOptions.OrderBy(delegate(Color c)
							{
								Color.RGBToHSV(c, out var _, out var S, out var V);
								return S + V;
							}));
						}
					}
				}
				else if (cgMelanin.naturalMelanin)
				{
					foreach (GeneDef geneDef in PawnSkinColors.SkinColorGenesInOrder)
					{
						if (geneDef.skinColorBase.HasValue)
						{
							availableColors.Add(geneDef.skinColorBase.Value);
						}
					}
				}
				else
				{
					for (int i = 0; i < PawnSkinColors.SkinColorGenesInOrder.Count; i++)
					{
						float currentMelanin = Mathf.Lerp(cgMelanin.minMelanin, cgMelanin.maxMelanin, 1f / (float)PawnSkinColors.SkinColorGenesInOrder.Count * (float)i);
						int nextIndex = PawnSkinColors.SkinColorGenesInOrder.FirstIndexOf((GeneDef gd) => gd.minMelanin >= currentMelanin);
						GeneDef nextGene = PawnSkinColors.SkinColorGenesInOrder[nextIndex];
						if (nextIndex == 0)
						{
							availableColors.Add(nextGene.skinColorBase.Value);
							continue;
						}
						GeneDef lastGene = PawnSkinColors.SkinColorGenesInOrder[nextIndex - 1];
						availableColors.Add(Color.Lerp(lastGene.skinColorBase.Value, nextGene.skinColorBase.Value, Mathf.InverseLerp(lastGene.minMelanin, nextGene.minMelanin, currentMelanin)));
					}
				}
			}
			else
			{
				cgCustomAlien.GetInfo(out var channel, out var firstCustom);
				foreach (AlienPartGenerator.ColorChannelGeneratorCategory entriesCustom in alienRaceDef.alienRace.generalSettings.alienPartGenerator.colorChannels.Find((AlienPartGenerator.ColorChannelGenerator ccg) => ccg.name == channel).entries)
				{
					availableColors.AddRange(AvailableColors(firstCustom ? entriesCustom.first : entriesCustom.second));
				}
			}
		}
		else
		{
			foreach (ColorGenerator generator in accg.AvailableGenerators(pawn))
			{
				availableColors.AddRange(AvailableColors(generator));
			}
			availableColors.AddRange(accg.AvailableColors(pawn));
		}
		return availableColors;
	}

	public static void DoRaceTabs(Rect inRect)
	{
		raceTabs.Clear();
		raceTabs.Add(new TabRecord("HAR.Colors".Translate(), delegate
		{
			curRaceTab = RaceTab.COLORS;
		}, curRaceTab == RaceTab.COLORS));
		raceTabs.Add(new TabRecord("HAR.BodyAddons".Translate(), delegate
		{
			curRaceTab = RaceTab.BODY_ADDONS;
		}, curRaceTab == RaceTab.BODY_ADDONS));
		Widgets.DrawMenuSection(inRect);
		TabDrawer.DrawTabs(inRect, raceTabs);
		switch (curRaceTab)
		{
		case RaceTab.BODY_ADDONS:
			DrawBodyAddonTab(inRect);
			break;
		case RaceTab.COLORS:
			DrawColorChannelTab(inRect);
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
	}

	public static void DrawBodyAddonTab(Rect inRect)
	{
		List<AlienPartGenerator.BodyAddon> bodyAddons = alienRaceDef.alienRace.generalSettings.alienPartGenerator.bodyAddons.Concat(Utilities.UniversalBodyAddons).ToList();
		DoAddonList(inRect.LeftPartPixels(260f), bodyAddons);
		inRect.xMin += 260f;
		if (selectedIndexAddons != -1)
		{
			AlienPartGenerator.BodyAddon addon = bodyAddons[selectedIndexAddons];
			if (addon.userCustomizable && addon.CanDrawAddonStatic(pawn))
			{
				DoAddonInfo(inRect, addon, bodyAddons);
			}
			else
			{
				selectedIndexAddons = -1;
			}
		}
	}

	private static void DoAddonList(Rect inRect, List<AlienPartGenerator.BodyAddon> addons)
	{
		int usableCount = addons.Count((AlienPartGenerator.BodyAddon ba) => ba.userCustomizable);
		if (selectedIndexAddons >= addons.Count)
		{
			selectedIndexAddons = -1;
		}
		Widgets.DrawMenuSection(inRect);
		if (usableCount <= 0)
		{
			GUI.color = Color.gray;
			Text.Anchor = TextAnchor.MiddleCenter;
			Widgets.Label(inRect, "HAR.NoAddonsForList".Translate());
			Text.Anchor = TextAnchor.UpperLeft;
			GUI.color = Color.white;
			return;
		}
		Rect viewRect = new Rect(0f, 0f, 250f, usableCount * 54 + 4);
		Widgets.BeginScrollView(inRect, ref addonsScrollPos, viewRect);
		int usableIndex = -1;
		for (int i = 0; i < addons.Count; i++)
		{
			if (!addons[i].userCustomizable || !addons[i].CanDrawAddonStatic(pawn))
			{
				continue;
			}
			usableIndex++;
			Rect rect = new Rect(10f, (float)usableIndex * 54f + 4f, 240f, 50f).ContractedBy(2f);
			if (i == selectedIndexAddons)
			{
				Widgets.DrawOptionSelected(rect);
			}
			else
			{
				GUI.color = Widgets.WindowBGFillColor;
				GUI.DrawTexture(rect, (Texture)BaseContent.WhiteTex);
				GUI.color = Color.white;
				bool groupSelected = false;
				int index = usableIndex;
				while (index >= 0 && addons[index].linkVariantIndexWithPrevious)
				{
					index--;
					if (selectedIndexAddons == index)
					{
						groupSelected = true;
					}
				}
				for (index = i + 1; index <= addons.Count - 1 && addons[index].linkVariantIndexWithPrevious; index++)
				{
					if (selectedIndexAddons == index)
					{
						groupSelected = true;
					}
				}
				if (groupSelected)
				{
					GUI.color = new ColorInt(135, 135, 135).ToColor;
					Widgets.DrawBox(rect);
					GUI.color = Color.white;
				}
			}
			Widgets.DrawHighlightIfMouseover(rect);
			if (Widgets.ButtonInvisible(rect))
			{
				selectedIndexAddons = i;
				SoundDefOf.Click.PlayOneShotOnCamera();
			}
			Rect position = rect.LeftPartPixels(rect.height).ContractedBy(2f);
			int addonVariant = alienComp.addonVariants[i];
			Texture2D image = ContentFinder<Texture2D>.Get(addons[i].GetPath(pawn, ref addonVariant, addonVariant) + "_south");
			GUI.color = Widgets.MenuSectionBGFillColor;
			GUI.DrawTexture(position, (Texture)BaseContent.WhiteTex);
			GUI.color = Color.white;
			GUI.DrawTexture(position, (Texture)image);
			rect.xMin += rect.height;
			Widgets.Label(rect.ContractedBy(4f), addons[i].Name);
			if (addons[i].linkVariantIndexWithPrevious)
			{
				GUI.color = new ColorInt(135, 135, 135).ToColor;
				GUI.DrawTexture(new Rect(rect.x - rect.height - 6f, rect.center.y, 6f, 2f), (Texture)BaseContent.WhiteTex);
				GUI.DrawTexture(new Rect(rect.x - rect.height - 6f, (usableIndex - 1) * 54 + 27, 6f, 2f), (Texture)BaseContent.WhiteTex);
				GUI.DrawTexture(new Rect(rect.x - rect.height - 6f, (usableIndex - 1) * 54 + 27, 2f, 56f), (Texture)BaseContent.WhiteTex);
				GUI.color = Color.white;
			}
			AlienPartGenerator.ExposableValueTuple<Color, Color> channelColors = alienComp.GetChannel(addons[i].ColorChannel);
			(Color, Color) colors = (alienComp.addonColors[i].first ?? addons[i].colorOverrideOne ?? channelColors.first, alienComp.addonColors[i].second ?? addons[i].colorOverrideTwo ?? channelColors.second);
			Rect colorRect = new Rect(rect.xMax - 44f, rect.yMax - 22f, 18f, 18f);
			Widgets.DrawLightHighlight(colorRect);
			Widgets.DrawBoxSolid(colorRect.ContractedBy(2f), colors.Item1);
			colorRect = new Rect(rect.xMax - 22f, rect.yMax - 22f, 18f, 18f);
			Widgets.DrawLightHighlight(colorRect);
			Widgets.DrawBoxSolid(colorRect.ContractedBy(2f), colors.Item2);
		}
		Widgets.EndScrollView();
	}

	private static void DoAddonInfo(Rect inRect, AlienPartGenerator.BodyAddon addon, List<AlienPartGenerator.BodyAddon> addons)
	{
		AlienPartGenerator.ExposableValueTuple<Color, Color> channelColors = alienComp.GetChannel(addon.ColorChannel);
		(Color, Color) colors = (alienComp.addonColors[selectedIndexAddons].first ?? addon.colorOverrideOne ?? channelColors.first, alienComp.addonColors[selectedIndexAddons].second ?? addon.colorOverrideTwo ?? channelColors.second);
		Rect viewRect;
		if (addon.allowColorOverride)
		{
			List<Color> firstColors = AvailableColors(addon);
			List<Color> secondColors = AvailableColors(addon, first: false);
			if (firstColors.Any() || secondColors.Any())
			{
				Rect colorsRect = inRect.BottomPart(0.4f);
				inRect.yMax -= colorsRect.height;
				Widgets.DrawMenuSection(colorsRect);
				Color clear = Color.clear;
				List<Color> list = (editingFirstColor ? firstColors : secondColors);
				List<Color> list2 = new List<Color>(1 + list.Count);
				list2.Add(clear);
				list2.AddRange(list);
				List<Color> availableColors = list2;
				colorsRect = colorsRect.ContractedBy(6f);
				Vector2 size = new Vector2(18f, 18f);
				viewRect = new Rect(0f, 0f, colorsRect.width - 16f, (Mathf.Ceil((float)availableColors.Count / ((colorsRect.width - 14f) / size.x)) + 1f) * size.y + 35f);
				Widgets.BeginScrollView(colorsRect, ref colorsScrollPos, viewRect);
				Rect headerRect = viewRect.TopPartPixels(30f).ContractedBy(4f);
				viewRect.yMin += 30f;
				Widgets.Label(headerRect, "HAR.Colors".Translate());
				if (firstColors.Any())
				{
					Rect colorRect = new Rect(headerRect.xMax - 44f, headerRect.y, 18f, 18f);
					Widgets.DrawLightHighlight(colorRect);
					Widgets.DrawHighlightIfMouseover(colorRect);
					Widgets.DrawBoxSolid(colorRect.ContractedBy(2f), colors.Item1);
					if (editingFirstColor)
					{
						Widgets.DrawBox(colorRect);
					}
					if (Widgets.ButtonInvisible(colorRect))
					{
						editingFirstColor = true;
					}
				}
				else
				{
					editingFirstColor = false;
				}
				if (secondColors.Any())
				{
					Rect colorRect = new Rect(headerRect.xMax - 22f, headerRect.y, 18f, 18f);
					Widgets.DrawLightHighlight(colorRect);
					Widgets.DrawHighlightIfMouseover(colorRect);
					Widgets.DrawBoxSolid(colorRect.ContractedBy(2f), colors.Item2);
					if (!editingFirstColor)
					{
						Widgets.DrawBox(colorRect);
					}
					if (Widgets.ButtonInvisible(colorRect))
					{
						editingFirstColor = false;
					}
				}
				else
				{
					editingFirstColor = true;
				}
				Rect randomizeRect = viewRect.TopPartPixels(30f).LeftHalf().ContractedBy(4f);
				viewRect.yMin += 30f;
				if (Widgets.ButtonText(randomizeRect, "HAR.RandomizeColors".Translate()))
				{
					AlienPartGenerator.ExposableValueTuple<Color, Color> newlyGeneratedColors = alienComp.GenerateChannel(alienRaceDef.alienRace.generalSettings.alienPartGenerator.colorChannels.Find((AlienPartGenerator.ColorChannelGenerator ccg) => ccg.name == addon.ColorChannel));
					if (editingFirstColor)
					{
						alienComp.addonColors[selectedIndexAddons].first = newlyGeneratedColors.first;
					}
					else
					{
						alienComp.addonColors[selectedIndexAddons].second = newlyGeneratedColors.second;
					}
					PortraitsCache.SetDirty(pawn);
				}
				Vector2 pos = new Vector2(0f, 60f);
				for (int i = 0; i < availableColors.Count; i++)
				{
					Color color = availableColors[i];
					Rect rect = new Rect(pos, size).ContractedBy(1f);
					Widgets.DrawLightHighlight(rect);
					Widgets.DrawHighlightIfMouseover(rect);
					if (i == 0)
					{
						Widgets.DrawTextureFitted(rect.ContractedBy(1f), ClearTex, 1f);
					}
					else
					{
						Widgets.DrawBoxSolid(rect.ContractedBy(1f), color);
					}
					if (editingFirstColor)
					{
						if (colors.Item1.IndistinguishableFrom(color))
						{
							Widgets.DrawBox(rect);
						}
						if (Widgets.ButtonInvisible(rect))
						{
							alienComp.addonColors[selectedIndexAddons].first = ((i == 0) ? ((Color?)null) : new Color?(color));
							PortraitsCache.SetDirty(pawn);
						}
					}
					else
					{
						if (colors.Item2.IndistinguishableFrom(color))
						{
							Widgets.DrawBox(rect);
						}
						if (Widgets.ButtonInvisible(rect))
						{
							alienComp.addonColors[selectedIndexAddons].second = ((i == 0) ? ((Color?)null) : new Color?(color));
							PortraitsCache.SetDirty(pawn);
						}
					}
					pos.x += size.x;
					if (pos.x + size.x >= viewRect.xMax)
					{
						pos.y += size.y;
						pos.x = 0f;
					}
				}
				Widgets.EndScrollView();
			}
		}
		int variantCount = addon.VariantCountMax;
		int countPerRow = 4;
		float width = inRect.width - 20f;
		float itemSize;
		for (itemSize = width / (float)countPerRow; itemSize > 92f; itemSize = width / (float)countPerRow)
		{
			countPerRow++;
		}
		viewRect = new Rect(0f, 0f, width, Mathf.Ceil((float)variantCount / (float)countPerRow) * itemSize);
		Widgets.DrawMenuSection(inRect);
		Widgets.BeginScrollView(inRect, ref variantsScrollPos, viewRect);
		for (int i2 = 0; i2 < variantCount; i2++)
		{
			Rect rect2 = new Rect((float)(i2 % countPerRow) * itemSize, Mathf.Floor((float)i2 / (float)countPerRow) * itemSize, itemSize, itemSize).ContractedBy(2f);
			int index = i2;
			GUI.color = Widgets.WindowBGFillColor;
			GUI.DrawTexture(rect2, (Texture)BaseContent.WhiteTex);
			GUI.color = Color.white;
			Widgets.DrawHighlightIfMouseover(rect2);
			if (alienComp.addonVariants[selectedIndexAddons] == i2)
			{
				Widgets.DrawBox(rect2);
			}
			string addonPath = addon.GetPath(pawn, ref index, i2);
			Texture2D image = ContentFinder<Texture2D>.Get(addonPath + "_south", reportFailure: false);
			if (image != null)
			{
				GUI.DrawTexture(rect2, (Texture)image);
			}
			if (Widgets.ButtonInvisible(rect2))
			{
				alienComp.addonVariants[selectedIndexAddons] = i2;
				index = selectedIndexAddons;
				while (index >= 0 && addons[index].linkVariantIndexWithPrevious)
				{
					index--;
					alienComp.addonVariants[index] = i2;
				}
				for (index = selectedIndexAddons + 1; index <= addons.Count - 1 && addons[index].linkVariantIndexWithPrevious; index++)
				{
					alienComp.addonVariants[index] = i2;
				}
			}
		}
		Widgets.EndScrollView();
	}

	public static void DrawColorChannelTab(Rect inRect)
	{
		List<AlienPartGenerator.ColorChannelGenerator> channels = alienRaceDef.alienRace.generalSettings.alienPartGenerator.colorChannels;
		DoChannelList(inRect.LeftPartPixels(260f), channels);
		inRect.xMin += 260f;
		if (selectedIndexChannels != -1)
		{
			DoChannelInfo(inRect, channels[selectedIndexChannels], channels);
		}
	}

	private static void DoChannelList(Rect inRect, List<AlienPartGenerator.ColorChannelGenerator> channels)
	{
		if (selectedIndexChannels >= channels.Count)
		{
			selectedIndexChannels = -1;
		}
		Widgets.DrawMenuSection(inRect);
		if (channels.Count <= 0)
		{
			GUI.color = Color.gray;
			Text.Anchor = TextAnchor.MiddleCenter;
			Widgets.Label(inRect, "HAR.NoColorsForList".Translate());
			Text.Anchor = TextAnchor.UpperLeft;
			GUI.color = Color.white;
			return;
		}
		Rect viewRect = new Rect(0f, 0f, 250f, channels.Count * 54 + 4);
		Widgets.BeginScrollView(inRect, ref channelsScrollPos, viewRect);
		for (int i = 0; i < channels.Count; i++)
		{
			AlienPartGenerator.ColorChannelGenerator channel = channels[i];
			Rect rect = new Rect(10f, (float)i * 54f + 4f, 240f, 50f).ContractedBy(2f);
			if (i == selectedIndexChannels)
			{
				Widgets.DrawOptionSelected(rect);
			}
			else
			{
				GUI.color = Widgets.WindowBGFillColor;
				GUI.DrawTexture(rect, (Texture)BaseContent.WhiteTex);
				GUI.color = Color.white;
			}
			Widgets.DrawHighlightIfMouseover(rect);
			if (Widgets.ButtonInvisible(rect))
			{
				selectedIndexChannels = i;
				if (channel.name == "hair")
				{
					editingFirstColor = false;
				}
				SoundDefOf.Click.PlayOneShotOnCamera();
			}
			AlienPartGenerator.ExposableValueTuple<Color, Color> channelColors = alienComp.GetChannel(channel.name);
			(Color, Color) colors = (channelColors.first, channelColors.second);
			Rect position = rect.LeftPartPixels(rect.height).ContractedBy(2f);
			Widgets.DrawLightHighlight(position);
			Widgets.DrawBoxSolid(position, colors.Item1);
			if (i == selectedIndexChannels && editingFirstColor)
			{
				Widgets.DrawBox(position);
			}
			Text.Anchor = TextAnchor.MiddleCenter;
			Widgets.Label(rect, channel.name);
			Text.Anchor = TextAnchor.UpperLeft;
			rect.xMin += rect.height;
			position = rect.RightPartPixels(rect.height).ContractedBy(2f);
			Widgets.DrawLightHighlight(position);
			Widgets.DrawBoxSolid(position, colors.Item2);
			if (i == selectedIndexChannels && !editingFirstColor)
			{
				Widgets.DrawBox(position);
			}
		}
		Widgets.EndScrollView();
	}

	private static void DoChannelInfo(Rect inRect, AlienPartGenerator.ColorChannelGenerator channel, List<AlienPartGenerator.ColorChannelGenerator> channels)
	{
		List<Color> firstColors = AvailableColors(channel, first: true);
		List<Color> secondColors = AvailableColors(channel, first: false);
		AlienPartGenerator.ExposableValueTuple<Color, Color> channelColors = alienComp.GetChannel(channel.name);
		(Color, Color) colors = (channelColors.first, channelColors.second);
		if (channel.name == "hair" && editingFirstColor)
		{
			curMainTab = MainTab.CHARACTER;
			CachedData.stationCurTab(instance) = Dialog_StylingStation.StylingTab.Hair;
			if (secondColors.Any())
			{
				editingFirstColor = false;
			}
			else
			{
				selectedIndexChannels = -1;
			}
		}
		if (!firstColors.Any() && !secondColors.Any())
		{
			return;
		}
		Rect colorsRect = inRect;
		inRect.yMax -= colorsRect.height;
		Widgets.DrawMenuSection(colorsRect);
		List<Color> availableColors = (editingFirstColor ? firstColors : secondColors);
		colorsRect = colorsRect.ContractedBy(6f);
		Vector2 size = new Vector2(18f, 18f);
		Rect viewRect = new Rect(0f, 0f, colorsRect.width - 16f, channelColorViewRectHeight);
		Widgets.BeginScrollView(colorsRect, ref colorsScrollPos, viewRect);
		channelColorViewRectHeight = 0f;
		Rect headerRect = viewRect.TopPartPixels(30f).ContractedBy(4f);
		viewRect.yMin += 30f;
		channelColorViewRectHeight += 30f;
		Widgets.Label(headerRect, "HAR.Colors".Translate());
		if (firstColors.Any())
		{
			Rect colorRect = new Rect(headerRect.xMax - 44f - 7f, headerRect.y, 18f, 18f);
			Widgets.DrawLightHighlight(colorRect);
			Widgets.DrawHighlightIfMouseover(colorRect);
			Widgets.DrawBoxSolid(colorRect.ContractedBy(2f), colors.Item1);
			if (editingFirstColor)
			{
				Widgets.DrawBox(colorRect);
			}
			if (Widgets.ButtonInvisible(colorRect))
			{
				editingFirstColor = true;
			}
			if (channel.name == "hair")
			{
				colorRect.y += colorRect.height * 0.5f;
				colorRect.x += colorRect.width * 0.5f;
				Widgets.DrawTextureFitted(colorRect, ChainVanillaTex, 1.5f);
				if (Mouse.IsOver(colorRect))
				{
					TooltipHandler.TipRegion(colorRect, new TipSignal("HAR.HairOneInfo".Translate()));
				}
			}
			else
			{
				LinkedIndicator(colorRect, first: true);
			}
		}
		else
		{
			editingFirstColor = false;
		}
		if (secondColors.Any())
		{
			Rect colorRect = new Rect(headerRect.xMax - 22f, headerRect.y, 18f, 18f);
			Widgets.DrawLightHighlight(colorRect);
			Widgets.DrawHighlightIfMouseover(colorRect);
			Widgets.DrawBoxSolid(colorRect.ContractedBy(2f), colors.Item2);
			if (!editingFirstColor)
			{
				Widgets.DrawBox(colorRect);
			}
			if (Widgets.ButtonInvisible(colorRect))
			{
				editingFirstColor = false;
			}
			LinkedIndicator(colorRect, first: false);
		}
		else
		{
			editingFirstColor = true;
		}
		Rect randomizeRect = viewRect.TopPartPixels(30f).LeftHalf().ContractedBy(4f);
		viewRect.yMin += 30f;
		channelColorViewRectHeight += 30f;
		if (Widgets.ButtonText(randomizeRect, "HAR.RandomizeColors".Translate()))
		{
			AlienPartGenerator.ExposableValueTuple<Color, Color> newlyGeneratedColors = alienComp.GenerateChannel(channel);
			alienComp.OverwriteColorChannel(channel.name, editingFirstColor ? new Color?(newlyGeneratedColors.first) : ((Color?)null), (!editingFirstColor) ? new Color?(newlyGeneratedColors.second) : ((Color?)null));
		}
		Vector2 pos = new Vector2(0f, 65f);
		channelColorViewRectHeight += size.y + 5f;
		foreach (Color color in availableColors)
		{
			Rect rect = new Rect(pos, size).ContractedBy(1f);
			Widgets.DrawLightHighlight(rect);
			Widgets.DrawHighlightIfMouseover(rect);
			Widgets.DrawBoxSolid(rect.ContractedBy(1f), color);
			if (editingFirstColor)
			{
				if (colors.Item1.IndistinguishableFrom(color))
				{
					Widgets.DrawBox(rect);
				}
				if (Widgets.ButtonInvisible(rect))
				{
					alienComp.OverwriteColorChannel(channel.name, color);
				}
			}
			else
			{
				if (colors.Item2.IndistinguishableFrom(color))
				{
					Widgets.DrawBox(rect);
				}
				if (Widgets.ButtonInvisible(rect))
				{
					AlienPartGenerator.AlienComp obj = alienComp;
					string name = channel.name;
					Color? second = color;
					obj.OverwriteColorChannel(name, null, second);
				}
			}
			pos.x += size.x;
			if (pos.x + size.x >= viewRect.xMax)
			{
				pos.y += size.y;
				channelColorViewRectHeight += size.y;
				pos.x = 0f;
			}
		}
		Widgets.EndScrollView();
		void LinkedIndicator(Rect linkedRect, bool first)
		{
			linkedRect.y += linkedRect.height * 0.5f;
			linkedRect.x += linkedRect.width * 0.5f;
			List<string> linkedTo = new List<string>();
			if (alienComp.ColorChannelLinks.TryGetValue(channel.name, out var linkDataTo))
			{
				foreach (AlienPartGenerator.AlienComp.ColorChannelLinkData.ColorChannelLinkTargetData targetData in linkDataTo.targetsChannelOne)
				{
					LinkedToCheck(targetData, checkFirst: true);
					LinkedToCheck(targetData, checkFirst: false);
				}
			}
			List<string> linkedFrom = new List<string>();
			foreach (KeyValuePair<string, AlienPartGenerator.AlienComp.ColorChannelLinkData> colorChannelLink in alienComp.ColorChannelLinks)
			{
				colorChannelLink.Deconstruct(out var key, out var value);
				string baseChannel = key;
				AlienPartGenerator.AlienComp.ColorChannelLinkData linkDataFrom = value;
				foreach (AlienPartGenerator.AlienComp.ColorChannelLinkData.ColorChannelLinkTargetData targetData2 in first ? linkDataFrom.targetsChannelOne : linkDataFrom.targetsChannelTwo)
				{
					if (targetData2.targetChannel == channel.name)
					{
						AlienPartGenerator.ColorChannelGeneratorCategory entry = alienRaceDef.alienRace.generalSettings.alienPartGenerator.colorChannels.Find((AlienPartGenerator.ColorChannelGenerator ccg) => ccg.name == channel.name).entries[targetData2.categoryIndex];
						((ColorGenerator_CustomAlienChannel)(first ? entry.first : entry.second)).GetInfo(out key, out var firstOfChannel);
						linkedFrom.Add("HAR.LinkText".Translate(baseChannel.CapitalizeFirst(), (firstOfChannel ? "HAR.FirstColor" : "HAR.SecondColor").Translate()));
					}
				}
			}
			if (linkedTo.Any() || linkedFrom.Any())
			{
				Widgets.DrawTextureFitted(linkedRect, ChainTex, 1.5f);
				if (Mouse.IsOver(linkedRect))
				{
					StringBuilder sb = new StringBuilder();
					if (linkedTo.Any())
					{
						sb.AppendLine("HAR.LinkedTo".Translate());
						foreach (string s in linkedTo)
						{
							sb.AppendLine(s);
						}
					}
					if (linkedFrom.Any())
					{
						sb.AppendLine("HAR.LinkedFrom".Translate());
						foreach (string s2 in linkedFrom)
						{
							sb.AppendLine(s2);
						}
					}
					TooltipHandler.TipRegion(linkedRect, new TipSignal(sb.ToString()));
				}
			}
			void LinkedToCheck(AlienPartGenerator.AlienComp.ColorChannelLinkData.ColorChannelLinkTargetData colorChannelLinkTargetData, bool checkFirst)
			{
				AlienPartGenerator.ColorChannelGeneratorCategory entry2 = alienRaceDef.alienRace.generalSettings.alienPartGenerator.colorChannels.Find((AlienPartGenerator.ColorChannelGenerator ccg) => ccg.name == colorChannelLinkTargetData.targetChannel).entries[colorChannelLinkTargetData.categoryIndex];
				((ColorGenerator_CustomAlienChannel)(checkFirst ? entry2.first : entry2.second)).GetInfo(out var _, out var firstOfChannel2);
				if (first == firstOfChannel2)
				{
					linkedTo.Add("HAR.LinkText".Translate(colorChannelLinkTargetData.targetChannel.CapitalizeFirst(), (checkFirst ? "HAR.FirstColor" : "HAR.SecondColor").Translate()));
				}
			}
		}
	}

	public static void ResetPostfix(bool resetColors)
	{
		if (resetColors)
		{
			alienComp.addonVariants = addonVariants;
			alienComp.addonColors = addonColors;
			alienComp.ColorChannels = colorChannels;
			pawn.Drawer.renderer.SetAllGraphicsDirty();
			ConstructorPostfix(pawn);
		}
	}
}
