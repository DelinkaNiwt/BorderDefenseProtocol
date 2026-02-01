using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using VEF.Abilities;
using VEF.Utils;
using Verse;
using Verse.Sound;

namespace VanillaPsycastsExpanded.UI;

[StaticConstructorOnStartup]
public static class PsycastsUIUtility
{
	private static readonly Dictionary<MeditationFocusDef, Texture2D> meditationIcons;

	private static readonly float[][] abilityTreeXOffsets;

	public static Hediff_PsycastAbilities Hediff;

	public static CompAbilities CompAbilities;

	static PsycastsUIUtility()
	{
		abilityTreeXOffsets = new float[3][]
		{
			new float[1] { -18f },
			new float[2] { -47f, 11f },
			new float[3] { -69f, -18f, 33f }
		};
		meditationIcons = new Dictionary<MeditationFocusDef, Texture2D>();
		foreach (MeditationFocusDef allDef in DefDatabase<MeditationFocusDef>.AllDefs)
		{
			MeditationFocusExtension modExtension = allDef.GetModExtension<MeditationFocusExtension>();
			if (modExtension == null)
			{
				string arg = "Please ask " + (allDef.modContentPack?.ModMetaData?.AuthorsString ?? "its authors") + " to add one.";
				ModContentPack modContentPack = allDef.modContentPack;
				if (modContentPack != null && modContentPack.IsOfficialMod)
				{
					arg = "It's marked as an official DLC, and if that's the case then please report this to Vanilla Expanded team so it can receive an icon.";
				}
				Log.Warning($"MeditationFocusDef {allDef} does not have a MeditationFocusExtension, which means it will not have an icon in the Psycasts UI.\n{arg}");
				meditationIcons.Add(allDef, BaseContent.WhiteTex);
				continue;
			}
			meditationIcons.Add(allDef, ContentFinder<Texture2D>.Get(modExtension.icon));
			if (modExtension.statParts.NullOrEmpty())
			{
				continue;
			}
			foreach (StatPart_Focus statPart in modExtension.statParts)
			{
				statPart.focus = allDef;
				statPart.parentStat = StatDefOf.MeditationFocusStrength;
				StatDef meditationFocusGain = StatDefOf.MeditationFocusGain;
				if (meditationFocusGain.parts == null)
				{
					meditationFocusGain.parts = new List<StatPart>();
				}
				StatDefOf.MeditationFocusGain.parts.Add(statPart);
			}
		}
	}

	public static void LabelWithIcon(this Listing_Standard listing, Texture2D icon, string label)
	{
		float num = Text.CalcHeight(label, listing.ColumnWidth);
		Rect rect = listing.GetRect(num);
		float num2 = (float)icon.width * (num / (float)icon.height);
		GUI.DrawTexture(UIUtility.TakeLeftPart(ref rect, num2), (Texture)icon);
		rect.xMin += 3f;
		Widgets.Label(rect, label);
		listing.Gap(3f);
	}

	public static void StatDisplay(this Listing_Standard listing, Texture2D icon, StatDef stat, Thing thing)
	{
		listing.LabelWithIcon(icon, stat.LabelCap + ": " + stat.Worker.GetStatDrawEntryLabel(stat, thing.GetStatValue(stat), stat.toStringNumberSense, StatRequest.For(thing)));
	}

	public static Rect CenterRect(this Rect rect, Vector2 size)
	{
		return new Rect(rect.center - size / 2f, size);
	}

	public static Texture2D Icon(this MeditationFocusDef def)
	{
		return meditationIcons[def];
	}

	public static void DrawPathBackground(ref Rect rect, PsycasterPathDef def, bool altTex = false)
	{
		Texture2D texture2D = (altTex ? def.backgroundImage : def.altBackgroundImage);
		GUI.color = new ColorInt(97, 108, 122).ToColor;
		Widgets.DrawBox(rect.ExpandedBy(2f), 1, Texture2D.whiteTexture);
		GUI.color = Color.white;
		Rect rect2 = UIUtility.TakeBottomPart(ref rect, 30f);
		Widgets.DrawRectFast(rect2, Widgets.WindowBGFillColor);
		Text.Anchor = TextAnchor.MiddleCenter;
		Widgets.Label(rect2, def.LabelCap);
		GUI.DrawTexture(rect, (Texture)texture2D);
		Text.Anchor = TextAnchor.UpperLeft;
	}

	private static bool EnsureInit()
	{
		if (Hediff != null && CompAbilities != null)
		{
			return true;
		}
		Log.Error("[VPE] PsycastsUIUtility was used without being initialized.");
		return false;
	}

	public static void DoPathAbilities(Rect inRect, PsycasterPathDef path, Dictionary<AbilityDef, Vector2> abilityPos, Action<Rect, AbilityDef> doAbility)
	{
		if (!EnsureInit())
		{
			return;
		}
		foreach (AbilityDef ability in path.abilities)
		{
			List<AbilityDef> list = ability.Psycast()?.prerequisites;
			if (list == null || !abilityPos.ContainsKey(ability))
			{
				continue;
			}
			foreach (AbilityDef item in list.Where((AbilityDef abilityDef) => abilityPos.ContainsKey(abilityDef)))
			{
				Widgets.DrawLine(abilityPos[ability], abilityPos[item], CompAbilities.HasAbility(item) ? Color.white : Color.grey, 2f);
			}
		}
		for (int num = 0; num < path.abilityLevelsInOrder.Length; num++)
		{
			Rect rect = new Rect(inRect.x, inRect.y + (float)(path.MaxLevel - 1 - num) * inRect.height / (float)path.MaxLevel + 10f, inRect.width, inRect.height / 5f);
			AbilityDef[] array = path.abilityLevelsInOrder[num];
			for (int num2 = 0; num2 < array.Length; num2++)
			{
				Rect arg = new Rect(rect.x + rect.width / 2f + abilityTreeXOffsets[array.Length - 1][num2], rect.y, 36f, 36f);
				AbilityDef val = array[num2];
				if (val != PsycasterPathDef.Blank)
				{
					abilityPos[val] = arg.center;
					doAbility(arg, val);
				}
			}
		}
	}

	public static void DrawAbility(Rect inRect, AbilityDef ability)
	{
		Color color = (Mouse.IsOver(inRect) ? GenUI.MouseoverColor : Color.white);
		MouseoverSounds.DoRegion(inRect, SoundDefOf.Mouseover_Command);
		GUI.color = color;
		GUI.DrawTexture(inRect, (Texture)Command.BGTexShrunk);
		GUI.color = Color.white;
		GUI.DrawTexture(inRect, (Texture)ability.icon);
	}
}
