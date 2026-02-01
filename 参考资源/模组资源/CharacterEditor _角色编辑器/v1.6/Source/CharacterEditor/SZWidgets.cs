using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace CharacterEditor;

internal static class SZWidgets
{
	private static int iUID = -1;

	internal const string CO_ITEMICON = "iconTex";

	internal static bool bCountOpen = false;

	internal static bool bQualityOpen = false;

	internal static bool bStuffOpen = false;

	internal static bool bStyleOpen = false;

	internal static List<string> lSimilar = new List<string>();

	internal static string sFind = "";

	internal static string sFindOld = "";

	internal static string sFindTemp = "";

	internal static string tDefName = null;

	internal static int waitTimer = 0;

	internal static int iLabelId = -1;

	internal static bool bToggleSearch = false;

	internal static bool bFocusOnce = true;

	private static int iShowId = 0;

	private static string tempText = "";

	private static int iTempTextID = 1000;

	internal static bool bRemoveOnClick = false;

	internal static Color RemoveColor => bRemoveOnClick ? Color.red : Color.white;

	private static void DefSelectorSimpleBase<T>(Rect r, int w, HashSet<T> l, ref T def, string labelInfo, Func<T, string> labelGetter, Action<T> onSelect, bool hasLR = false, bool hasLIcon = false, Action<T> onLIcon = null, bool drawLabel = true) where T : Def
	{
		r.width = w;
		Text.Font = GameFont.Small;
		if (drawLabel)
		{
			Rect rect = r.RectLabel(hasLR, hasLIcon, hasRightIcon: false);
			LabelBackground(rect, labelInfo + labelGetter(def), ColorTool.colAsche);
			string toolTip = FLabel.DefDescription(def);
			ButtonInvisibleMouseOver(rect, delegate
			{
				FloatMenuOnRect(l, labelGetter, onSelect);
			}, toolTip);
		}
		if (!hasLR)
		{
			return;
		}
		if (Widgets.ButtonImage(RectPrevious(r), ContentFinder<Texture2D>.Get("bbackward")))
		{
			def = l.GetPrev(def);
			if (onSelect != null)
			{
				onSelect(def);
			}
		}
		if (Widgets.ButtonImage(RectNext(r), ContentFinder<Texture2D>.Get("bforward")))
		{
			def = l.GetNext(def);
			if (onSelect != null)
			{
				onSelect(def);
			}
		}
	}

	internal static void DefSelectorSimpleBullet<T>(Rect r, int posX, int posY, int w, HashSet<T> l, ref T def, string labelInfo, Func<T, string> labelGetter, Action<T> onSelect, bool hasLR = false, Texture2D texLIcon = null, float angle = 0f, Action<T> onLIcon = null, bool drawLabel = true) where T : Def
	{
		DefSelectorSimpleBase(r, w, l, ref def, labelInfo, labelGetter, onSelect, hasLR, texLIcon != null, onLIcon, drawLabel);
		if (!(texLIcon != null))
		{
			return;
		}
		if (!drawLabel)
		{
			ButtonInvisibleMouseOver(r, delegate
			{
				FloatMenuOnRect(l, labelGetter, onLIcon);
			}, FLabel.DefDescription(def));
		}
		else
		{
			ButtonInvisibleVar(r, onLIcon, def, def.STooltip());
		}
	}

	internal static void DefSelectorSimpleTex<T>(Rect r, int w, HashSet<T> l, ref T def, string labelInfo, Func<T, string> labelGetter, Action<T> onSelect, bool hasLR = false, Texture2D texLIcon = null, Action<T> onLIcon = null, bool drawLabel = true, string tooltip = "") where T : Def
	{
		DefSelectorSimpleBase(r, w, l, ref def, labelInfo, labelGetter, onSelect, hasLR, texLIcon != null, onLIcon, drawLabel);
		if (!tooltip.NullOrEmpty())
		{
			TooltipHandler.TipRegion(r, tooltip);
		}
		if (!(texLIcon != null))
		{
			return;
		}
		Rect rect = r.RectIconLeft(hasLR);
		if (!drawLabel)
		{
			Image(r.RectIconLeft(hasLR), texLIcon);
			ButtonInvisibleMouseOver(rect, delegate
			{
				FloatMenuOnRect(l, labelGetter, onLIcon);
			}, FLabel.DefDescription(def));
		}
		else
		{
			ButtonImageVar(rect, texLIcon, onLIcon, def, def.STooltip());
		}
	}

	internal static void DefSelectorSimple<T>(Rect r, int w, HashSet<T> l, ref T def, string labelInfo, Func<T, string> labelGetter, Action<T> onSelect, bool hasLR = false, string texLIcon = null, Action<T> onLIcon = null, bool drawLabel = true, string tooltip = "") where T : Def
	{
		DefSelectorSimpleBase(r, w, l, ref def, labelInfo, labelGetter, onSelect, hasLR, texLIcon != null, onLIcon, drawLabel);
		if (!tooltip.NullOrEmpty())
		{
			TooltipHandler.TipRegion(r, tooltip);
		}
		if (texLIcon == null)
		{
			return;
		}
		Rect rect = r.RectIconLeft(hasLR);
		if (!drawLabel)
		{
			Image(r.RectIconLeft(hasLR), texLIcon);
			ButtonInvisibleMouseOver(rect, delegate
			{
				FloatMenuOnRect(l, labelGetter, onLIcon);
			}, FLabel.DefDescription(def));
		}
		else
		{
			ButtonImageVar(rect, texLIcon, onLIcon, def, def.STooltip());
		}
	}

	internal static void NonDefSelectorSimple<T>(Rect r, int w, HashSet<T> l, ref T val, string labelInfo, Func<T, string> labelGetter, Action<T> onSelect, bool hasLR = false, string texLIcon = null, Action<T> onLIcon = null)
	{
		r.width = w;
		Text.Font = GameFont.Small;
		bool flag = texLIcon != null;
		Rect rect = r.RectLabel(hasLR, flag, hasRightIcon: false);
		LabelBackground(rect, labelInfo + labelGetter(val), ColorTool.colAsche);
		ButtonInvisibleMouseOver(rect, delegate
		{
			FloatMenuOnRect(l, labelGetter, onSelect);
		});
		if (hasLR)
		{
			if (Widgets.ButtonImage(RectPrevious(r), ContentFinder<Texture2D>.Get("bbackward")))
			{
				val = l.GetPrev(val);
				if (onSelect != null)
				{
					onSelect(val);
				}
			}
			if (Widgets.ButtonImage(RectNext(r), ContentFinder<Texture2D>.Get("bforward")))
			{
				val = l.GetNext(val);
				if (onSelect != null)
				{
					onSelect(val);
				}
			}
		}
		if (flag)
		{
			ButtonImageVar(r.RectIconLeft(hasLR), texLIcon, onLIcon, val);
		}
	}

	internal static Rect RectPlusY(this Rect rect, int y)
	{
		return new Rect(rect.x, rect.y + (float)y, rect.width, rect.height);
	}

	internal static Rect RectLablelI(this Rect rect, int inputW)
	{
		return new Rect(rect.x, rect.y, rect.width - (float)inputW - rect.height * 2f, rect.height);
	}

	internal static Rect RectInput(this Rect rect, int inputW)
	{
		return new Rect(rect.x + rect.width - rect.height - (float)inputW, rect.y, inputW, rect.height);
	}

	internal static Rect RectMinus(this Rect rect, int inputW)
	{
		return new Rect(rect.x + rect.width - rect.height * 2f - (float)inputW, rect.y, rect.height, rect.height);
	}

	internal static Rect RectPlus(this Rect rect)
	{
		return new Rect(rect.x + rect.width - rect.height, rect.y, rect.height, rect.height);
	}

	internal static Rect RectSlider(this Rect rect)
	{
		return new Rect(rect.x, rect.y + rect.height, rect.width, rect.height);
	}

	internal static Rect RectLabel(this Rect rect, bool inEditMode, bool hasLeftIcon, bool hasRightIcon)
	{
		return new Rect(rect.x + (float)OffsetEditLeft(inEditMode) + OffsetIcon(hasLeftIcon, rect.height), rect.y, rect.width - (float)OffsetEditBoth(inEditMode) - OffsetIcon(hasLeftIcon, rect.height) - OffsetIcon(hasRightIcon, rect.height), rect.height);
	}

	internal static Rect RectIconLeft(this Rect rect, bool inEditMode)
	{
		return new Rect(rect.x + (float)OffsetEditLeft(inEditMode), rect.y, rect.height, rect.height);
	}

	internal static Rect RectIconRight(this Rect rect, bool inEditMode)
	{
		return new Rect(rect.x + rect.width - (float)OffsetEditLeft(inEditMode) - rect.height, rect.y, rect.height, rect.height);
	}

	private static int OffsetEditLeft(bool inEditMode)
	{
		return inEditMode ? 21 : 0;
	}

	private static int OffsetEditBoth(bool inEditMode)
	{
		return inEditMode ? 42 : 0;
	}

	private static float OffsetIcon(bool hasIcon, float h)
	{
		return hasIcon ? h : 0f;
	}

	private static void GetEditRect(Rect rLabel, out Rect rValue, out Rect rLeft)
	{
		rLeft = new Rect(rLabel);
		rLeft.width -= 80f;
		rValue = new Rect(rLabel);
		rValue.x += rLeft.width;
		rValue.width = 80f;
	}

	private static void GetEditRects(Rect rLabel, string label, out Rect rValue, out Rect rLeft, out Rect rRight)
	{
		Vector2 vector = Text.CalcSize(label);
		rValue = new Rect(rLabel);
		rValue.x += vector.x + 15f;
		rValue.width = 80f;
		rLeft = new Rect(rLabel);
		rLeft.width = vector.x + 15f;
		rRight = new Rect(rLabel);
		rRight.x = rValue.x + rValue.width;
		rRight.width = rLabel.width - rRight.x + (float)OffsetEditLeft(inEditMode: true);
	}

	private static void ToggleParameterSlider(int id)
	{
		iUID = ((iUID == id) ? (-1) : id);
	}

	internal static void LabelFloatZeroFieldSlider(Listing_X view, int w, int id, Func<float?, string> FLabelValue, ref float? value, float min, float max, int decimals)
	{
		LabelFloatZeroFieldSlider(view.GetRect(22f), w, id, FLabelValue, ref value, min, max, decimals, view);
	}

	internal static void LabelFloatZeroFieldSlider(Rect r, int w, int id, Func<float?, string> FLabelValue, ref float? value, float min, float max, int decimals, Listing_X view = null)
	{
		bool flag = iUID == id;
		int inputW = 80;
		r.width = w;
		Rect rect = (flag ? r.RectLablelI(inputW) : r);
		Text.Font = GameFont.Small;
		LabelBackground(rect, FLabelValue(value), ColorTool.colAsche);
		ButtonInvisibleMouseOver(rect, delegate
		{
			ToggleParameterSlider(id);
		});
		if (flag)
		{
			if (Widgets.ButtonText(r.RectMinus(inputW), "-") && value.HasValue)
			{
				value -= 1f;
				value = (float)Math.Round(value.Value, decimals);
			}
			if (Widgets.ButtonText(r.RectPlus(), "+") && value.HasValue)
			{
				value += 1f;
				value = (float)Math.Round(value.Value, decimals);
			}
			Rect rect2 = view?.GetRect(r.height) ?? r.RectSlider();
			float value2 = (value.HasValue ? value.Value : 0f);
			value2 = FloatSlider(rect2, value2, min, max, decimals);
			value2 = FloatField(r.RectInput(inputW), value2, float.MinValue, float.MaxValue);
			value = value2;
		}
		view?.Gap(2f);
	}

	internal static void LabelFloatFieldSlider(Listing_X view, int w, int id, Func<float, string> FLabelValue, ref float value, float min, float max, int decimals)
	{
		LabelFloatFieldSlider(view.GetRect(22f), w, id, FLabelValue, ref value, min, max, decimals, view);
	}

	internal static void LabelFloatFieldSlider(Rect r, int w, int id, Func<float, string> FLabelValue, ref float value, float min, float max, int decimals, Listing_X view = null)
	{
		bool flag = iUID == id;
		int inputW = 80;
		r.width = w;
		Rect rect = (flag ? r.RectLablelI(inputW) : r);
		Text.Font = GameFont.Small;
		LabelBackground(rect, FLabelValue(value), ColorTool.colAsche);
		ButtonInvisibleMouseOver(rect, delegate
		{
			ToggleParameterSlider(id);
		});
		if (flag)
		{
			if (Widgets.ButtonText(r.RectMinus(inputW), "-"))
			{
				value -= 1f;
				value = (float)Math.Round(value, decimals);
			}
			if (Widgets.ButtonText(r.RectPlus(), "+"))
			{
				value += 1f;
				value = (float)Math.Round(value, decimals);
			}
			Rect rect2 = view?.GetRect(r.height) ?? r.RectSlider();
			value = FloatSlider(rect2, value, min, max, decimals);
			value = FloatField(r.RectInput(inputW), value, float.MinValue, float.MaxValue);
		}
		view?.Gap(2f);
	}

	internal static void LabelIntFieldSlider(Listing_X view, int w, int id, Func<int, string> FLabelValue, ref int value, int min, int max)
	{
		LabelIntFieldSlider(view.GetRect(22f), w, id, FLabelValue, ref value, min, max, view);
	}

	internal static void LabelIntFieldSlider(Rect r, int w, int id, Func<int, string> FLabelValue, ref int value, int min, int max, Listing_X view = null)
	{
		bool flag = iUID == id;
		int inputW = 80;
		r.width = w;
		Rect rect = (flag ? r.RectLablelI(inputW) : r);
		Text.Font = GameFont.Small;
		LabelBackground(rect, FLabelValue(value), ColorTool.colAsche);
		ButtonInvisibleMouseOver(rect, delegate
		{
			ToggleParameterSlider(id);
		});
		if (flag)
		{
			if (Widgets.ButtonText(r.RectMinus(inputW), "-"))
			{
				value--;
			}
			if (Widgets.ButtonText(r.RectPlus(), "+"))
			{
				value++;
			}
			Rect rect2 = view?.GetRect(r.height) ?? r.RectSlider();
			value = IntSlider(rect2, value, min, max);
			value = IntField(r.RectInput(inputW), value, int.MinValue, int.MaxValue);
		}
		view?.Gap(2f);
	}

	internal static float FloatSlider(Rect rect, float value, float min, float max, int decimals)
	{
		return (float)Math.Round(Widgets.HorizontalSlider(rect, value, min, max), decimals);
	}

	internal static int IntSlider(Rect rect, int value, int min, int max)
	{
		return (int)Widgets.HorizontalSlider(rect, value, min, max);
	}

	internal static float FloatField(Rect rect, float value, float min, float max)
	{
		string text = value.ToString();
		if (text.EndsWith("."))
		{
			text += "0";
		}
		else if (!text.Contains("."))
		{
			text += ".0";
		}
		text = Widgets.TextField(rect, text, 32);
		if (text.EndsWith("."))
		{
			text += "0";
		}
		else if (!text.Contains("."))
		{
			text += ".0";
		}
		float result = 0f;
		float.TryParse(text, out result);
		value = ((result < min) ? min : ((!(result > max)) ? result : max));
		return value;
	}

	internal static int IntField(Rect rect, int value, int min, int max)
	{
		string text = value.ToString();
		text = Widgets.TextField(rect, text, 32);
		int result = 0;
		int.TryParse(text, out result);
		value = ((result < min) ? min : ((result <= max) ? result : max));
		return value;
	}

	private static string GetFormattedValue(string format, float value)
	{
		switch (format)
		{
		case "%":
			return " [" + Math.Round(100f * value, 0) + " %]";
		case "s":
			return " [" + value + " s]";
		case "ticks":
			return " [" + value + " ticks]";
		case "rpm":
			return " [" + ((value == 0f) ? CharacterEditor.Label.INFINITE : Math.Round(60f / value * 60f, 0).ToString()) + " rpm]";
		case "cps":
			return " [" + ((value == 0f) ? CharacterEditor.Label.INFINITE : value.ToString()) + " cps]";
		case "cells":
			return " [" + value + " cells]";
		default:
			if (format.StartsWith("max"))
			{
				return " [" + value + "/" + format.SubstringFrom("max") + "]";
			}
			switch (format)
			{
			case "int":
				return " [" + (int)Math.Round(value) + "]";
			case "quadrum":
				return " [" + Enum.GetName(typeof(Quadrum), (int)value) + "]";
			case "addict":
				return " " + (100.0 - Math.Round(100f * value, 0)) + " %";
			case "high":
				return " " + value.ToStringPercent("F0");
			default:
				if (format.StartsWith("DEF"))
				{
					return " [" + format.SubstringFrom("DEF") + "]";
				}
				if (format.StartsWith("dauer"))
				{
					return " " + format.SubstringFrom("dauer");
				}
				if (format == "pain")
				{
					int num = (int)value;
					return (num == 0) ? CharacterEditor.Label.PAINLESS : ("PainCategory_" + HealthTool.ConvertSliderToPainCategory(num)).Translate().ToString();
				}
				if (format.StartsWith("comp"))
				{
					if (!format.Contains("%"))
					{
						return " " + value.ToStringPercent("F0") + " " + format.SubstringFrom("comp");
					}
					return " " + format.SubstringFrom("comp");
				}
				return " [" + value + "]";
			}
		}
	}

	internal static void FloatMenuOnButtonImage<T>(Rect rect, Texture2D tex, ICollection<T> l, Func<T, string> labelGetter, Action<T> action, string toolTip = "")
	{
		GUI.color = ((!Mouse.IsOver(rect)) ? Color.white : GenUI.MouseoverColor);
		GUI.DrawTexture(rect, (Texture)tex);
		GUI.color = Color.white;
		if (Widgets.ButtonInvisible(rect))
		{
			FloatMenuOnRect(l, labelGetter, action);
		}
		if (!toolTip.NullOrEmpty())
		{
			TooltipHandler.TipRegion(rect, toolTip);
		}
	}

	internal static void FloatMenuOnButtonInvisible<T>(Rect rect, ICollection<T> l, Func<T, string> labelGetter, Action<T> action, string toolTip = "")
	{
		if (Widgets.ButtonInvisible(rect))
		{
			FloatMenuOnRect(l, labelGetter, action);
		}
		if (!toolTip.NullOrEmpty())
		{
			TooltipHandler.TipRegion(rect, toolTip);
		}
	}

	internal static void FloatMenuOnButtonInvisibleColorDef(Rect rect, ICollection<ColorDef> l, Func<ColorDef, string> labelGetter, Action<ColorDef> action, string toolTip = "")
	{
		if (Widgets.ButtonInvisible(rect))
		{
			FloatMenuOnRectColorDef(l, labelGetter, action);
		}
		if (!toolTip.NullOrEmpty())
		{
			TooltipHandler.TipRegion(rect, toolTip);
		}
	}

	internal static void FloatMenuOnButtonStuffOrStyle<T>(Rect rectThing, Rect rectClickable, ICollection<T> l, Func<T, string> labelGetter, Selected s, Action<T> action) where T : Def
	{
		if (Mouse.IsOver(rectClickable))
		{
			Widgets.DrawHighlight(rectClickable);
		}
		if (typeof(T) == typeof(ThingStyleDef))
		{
			GUI.DrawTexture(rectThing, (Texture)IconForStyle(s));
			TooltipHandler.TipRegion(rectThing, s.style.STooltip());
		}
		else
		{
			GUI.DrawTexture(rectThing, (Texture)IconForStuff(s));
			TooltipHandler.TipRegion(rectThing, s.stuff.STooltip());
		}
		if (Widgets.ButtonInvisible(rectClickable))
		{
			FloatMenuOnRect(l, labelGetter, action, s);
		}
	}

	internal static void FloatMenuOnButtonText<T>(Rect rect, string curVal, ICollection<T> l, Func<T, string> labelGetter, Action<T> action, string toolTip = "")
	{
		if (Widgets.ButtonText(rect, curVal))
		{
			FloatMenuOnRect(l, labelGetter, action);
		}
		if (!toolTip.NullOrEmpty())
		{
			TooltipHandler.TipRegion(rect, toolTip);
		}
	}

	internal static void FloatMenuOnLabel<T>(Rect rect, Color color, ICollection<T> l, Func<T, string> labelGetter, T selected, Action<T> action, string toolTip = "")
	{
		LabelBackground(rect, labelGetter(selected), color);
		if (Mouse.IsOver(rect))
		{
			Widgets.DrawLightHighlight(rect);
		}
		FloatMenuOnButtonInvisible(new Rect(rect.x, rect.y, rect.width - 20f, rect.height), l, labelGetter, action, toolTip);
	}

	internal static void FloatMenuOnLabelAndImage<T>(Rect rect, Color imgColor, string texPath, Pawn pawnForImage, Color lblColor, ICollection<T> l, Func<T, string> labelGetter, T selected, Action<T> action, Action imgAction, bool showFloatMenu = true) where T : Def
	{
		Widgets.DrawBoxSolid(rect, imgColor);
		if (pawnForImage != null)
		{
			RenderTexture renderTexture = PortraitsCache.Get(pawnForImage, new Vector2(rect.width, rect.height), Rot4.South);
			GUI.DrawTexture(rect, (Texture)renderTexture);
		}
		else
		{
			Image(rect, texPath);
		}
		if (showFloatMenu)
		{
			FloatMenuOnLabel(new Rect(rect.x, rect.y - 20f, rect.width, 20f), lblColor, l, labelGetter, selected, action);
		}
		if (Widgets.ButtonInvisible(rect))
		{
			imgAction?.Invoke();
		}
	}

	internal static List<FloatMenuOption> FloatMenuOnRectColorDef(ICollection<ColorDef> l, Func<ColorDef, string> labelGetter, Action<ColorDef> action)
	{
		List<FloatMenuOption> list = new List<FloatMenuOption>();
		if (!l.EnumerableNullOrEmpty())
		{
			foreach (ColorDef element in l)
			{
				string label = labelGetter(element);
				FloatMenuOption floatMenuOption = new FloatMenuOption(label, delegate
				{
					if (action != null)
					{
						action(element);
					}
				});
				if (element != null)
				{
					floatMenuOption.SetFMOIcon(ContentFinder<Texture2D>.Get("bfavcolor"));
					floatMenuOption.iconColor = element.color;
					floatMenuOption.tooltip = element.STooltip();
				}
				list.Add(floatMenuOption);
			}
			WindowTool.Open(new FloatMenu(list));
		}
		return list;
	}

	internal static List<FloatMenuOption> FloatMenuOnRect<T>(ICollection<T> l, Func<T, string> labelGetter, Action<T> action, Selected s = null, bool doWindow = true)
	{
		List<FloatMenuOption> list = new List<FloatMenuOption>();
		if (!l.EnumerableNullOrEmpty())
		{
			foreach (T element in l)
			{
				string label = labelGetter(element);
				FloatMenuOption floatMenuOption = new FloatMenuOption(label, delegate
				{
					if (action != null)
					{
						action(element);
					}
				});
				if (element != null)
				{
					floatMenuOption.SetFMOIcon(element.GetTIcon(s));
					floatMenuOption.iconColor = element.GetTColor();
					floatMenuOption.tooltip = element.STooltip();
				}
				list.Add(floatMenuOption);
			}
			if (doWindow)
			{
				WindowTool.Open(new FloatMenu(list));
			}
		}
		return list;
	}

	internal static void FloatMixedMenuOnButtonImage<T1, T2>(Rect rect, Texture2D tex, List<T1> l1, List<T2> l2, Func<T1, string> labelGetter1, Func<T2, string> labelGetter2, Action<T1> action1, Action<T2> action2, string toolTip = "")
	{
		GUI.color = ((!Mouse.IsOver(rect)) ? Color.white : GenUI.MouseoverColor);
		GUI.DrawTexture(rect, (Texture)tex);
		GUI.color = Color.white;
		if (Widgets.ButtonInvisible(rect))
		{
			List<FloatMenuOption> list = FloatMenuOnRect(l1, labelGetter1, action1, null, doWindow: false);
			list.AddRange(FloatMenuOnRect(l2, labelGetter2, action2, null, doWindow: false));
			WindowTool.Open(new FloatMenu(list));
		}
		if (!toolTip.NullOrEmpty())
		{
			TooltipHandler.TipRegion(rect, toolTip);
		}
	}

	internal static Texture2D IconForStuff(Selected s)
	{
		return (s != null && s.stuff != null) ? Widgets.GetIconFor(s.stuff, s.stuff) : null;
	}

	internal static Texture2D IconForStyle(Selected s)
	{
		return (s != null && s.thingDef != null && s.stuff != null && s.style != null) ? Widgets.GetIconFor(s.thingDef, s.stuff, s.style) : null;
	}

	internal static Texture2D IconForStyleCustom(Selected s, ThingStyleDef style)
	{
		return (s != null && s.thingDef != null && s.stuff != null && style != null) ? Widgets.GetIconFor(s.thingDef, s.stuff, style) : null;
	}

	private static void SetFMOIcon(this FloatMenuOption fmo, Texture2D t)
	{
		if (t != null)
		{
			fmo.SetMemberValue("iconTex", t);
		}
	}

	internal static void FlipTextureHorizontally(Texture2D original)
	{
		Color[] pixels = original.GetPixels();
		Color[] array = new Color[pixels.Length];
		int width = original.width;
		int height = original.height;
		for (int i = 0; i < width; i++)
		{
			for (int j = 0; j < height; j++)
			{
				array[i + j * width] = pixels[width - i - 1 + j * width];
			}
		}
		original.SetPixels(array);
		original.Apply();
	}

	internal static void FlipTextureVertically(Texture2D original)
	{
		Color[] pixels = original.GetPixels();
		Color[] array = new Color[pixels.Length];
		int width = original.width;
		int height = original.height;
		for (int i = 0; i < width; i++)
		{
			for (int j = 0; j < height; j++)
			{
				array[i + j * width] = pixels[i + (height - j - 1) * width];
			}
		}
		original.SetPixels(array);
		original.Apply();
	}

	internal static void ButtonImageTex(Rect rect, Texture2D tex, Action action)
	{
		if (!(tex == null) && Widgets.ButtonImage(rect, tex))
		{
			action?.Invoke();
		}
	}

	internal static void ButtonImage(Rect rect, string texPath, Action action, string tooolTip = "")
	{
		if (!texPath.NullOrEmpty())
		{
			if (Widgets.ButtonImage(rect, ContentFinder<Texture2D>.Get(texPath)))
			{
				action?.Invoke();
			}
			if (!tooolTip.NullOrEmpty())
			{
				TooltipHandler.TipRegion(rect, tooolTip);
			}
		}
	}

	internal static void ButtonImage(float x, float y, float w, float h, string texPath, Action action, string toolTip = "", Color col = default(Color))
	{
		if (!texPath.NullOrEmpty())
		{
			Rect rect = new Rect(x, y, w, h);
			if ((!(col == default(Color))) ? Widgets.ButtonImage(rect, ContentFinder<Texture2D>.Get(texPath), col) : Widgets.ButtonImage(rect, ContentFinder<Texture2D>.Get(texPath)))
			{
				action?.Invoke();
			}
			if (!toolTip.NullOrEmpty())
			{
				TooltipHandler.TipRegion(rect, toolTip);
			}
		}
	}

	internal static void ButtonImageCol(Rect rect, string texPath, Action action, Color color, string toolTip = "")
	{
		if (!texPath.NullOrEmpty())
		{
			if (Widgets.ButtonImage(rect, ContentFinder<Texture2D>.Get(texPath), color))
			{
				action?.Invoke();
			}
			if (!toolTip.NullOrEmpty())
			{
				TooltipHandler.TipRegion(rect, toolTip);
			}
		}
	}

	internal static void ButtonHighlight(Rect rect, string texPath, Action<Color> action, Color color, string toolTip = "")
	{
		if (!texPath.NullOrEmpty())
		{
			if (Mouse.IsOver(rect))
			{
				Widgets.DrawBoxSolid(rect, new Color(color.r, color.g, color.b, 0.4f));
			}
			if (Widgets.ButtonImage(rect, ContentFinder<Texture2D>.Get(texPath), color, color))
			{
				action?.Invoke(color);
			}
			if (!toolTip.NullOrEmpty())
			{
				TooltipHandler.TipRegion(rect, toolTip);
			}
		}
	}

	internal static void ButtonHighlight(float x, float y, float w, float h, string texPath, Action<Color> action, Color color, string toolTip = "")
	{
		if (!texPath.NullOrEmpty())
		{
			Rect rect = new Rect(x, y, w, h);
			if (Mouse.IsOver(rect))
			{
				Widgets.DrawBoxSolid(rect, new Color(color.r, color.g, color.b, 0.4f));
			}
			if (Widgets.ButtonImage(rect, ContentFinder<Texture2D>.Get(texPath), color, color))
			{
				action?.Invoke(color);
			}
			if (!toolTip.NullOrEmpty())
			{
				TooltipHandler.TipRegion(rect, toolTip);
			}
		}
	}

	internal static void ButtonImageCol(float x, float y, float w, float h, string texPath, Action<Color> action, Color color, string toolTip = "")
	{
		if (!texPath.NullOrEmpty())
		{
			Rect rect = new Rect(x, y, w, h);
			if (Widgets.ButtonImage(rect, ContentFinder<Texture2D>.Get(texPath), color))
			{
				action?.Invoke(color);
			}
			if (!toolTip.NullOrEmpty())
			{
				TooltipHandler.TipRegion(rect, toolTip);
			}
		}
	}

	internal static void ButtonImageCol2<T>(Rect rect, string texPath, Action<T> action, T value, Color color, string toolTip = "")
	{
		if (!texPath.NullOrEmpty())
		{
			if (Widgets.ButtonImage(rect, ContentFinder<Texture2D>.Get(texPath), color))
			{
				action?.Invoke(value);
			}
			if (!toolTip.NullOrEmpty())
			{
				TooltipHandler.TipRegion(rect, toolTip);
			}
		}
	}

	internal static void ButtonImageVar<T>(Rect rect, Texture2D tex, Action<T> action, T value, string toolTip = "")
	{
		if (!(tex == null))
		{
			if (Widgets.ButtonImage(rect, tex))
			{
				action?.Invoke(value);
			}
			if (!toolTip.NullOrEmpty())
			{
				TooltipHandler.TipRegion(rect, toolTip);
			}
		}
	}

	internal static void ButtonImageVar<T>(Rect rect, string texPath, Action<T> action, T value, string toolTip = "")
	{
		if (!texPath.NullOrEmpty())
		{
			if (Widgets.ButtonImage(rect, ContentFinder<Texture2D>.Get(texPath)))
			{
				action?.Invoke(value);
			}
			if (!toolTip.NullOrEmpty())
			{
				TooltipHandler.TipRegion(rect, toolTip);
			}
		}
	}

	internal static void ButtonImageVar<T>(float x, float y, float w, float h, string texPath, Action<T> action, T value, string toolTip = "")
	{
		if (!texPath.NullOrEmpty())
		{
			Rect rect = new Rect(x, y, w, h);
			if (Widgets.ButtonImage(rect, ContentFinder<Texture2D>.Get(texPath)))
			{
				action?.Invoke(value);
			}
			if (!toolTip.NullOrEmpty())
			{
				TooltipHandler.TipRegion(rect, toolTip);
			}
		}
	}

	internal static void ButtonInvisible(Rect rect, Action action, string toolTip = "")
	{
		if (Widgets.ButtonInvisible(rect))
		{
			action?.Invoke();
		}
		if (!toolTip.NullOrEmpty())
		{
			TooltipHandler.TipRegion(rect, toolTip);
		}
	}

	internal static void ButtonInvisibleMouseOver(Rect rect, Action action, string toolTip = "")
	{
		if (Mouse.IsOver(rect))
		{
			Widgets.DrawHighlight(rect);
		}
		if (Widgets.ButtonInvisible(rect))
		{
			action?.Invoke();
		}
		if (!toolTip.NullOrEmpty())
		{
			TooltipHandler.TipRegion(rect, toolTip);
		}
	}

	internal static void ButtonInvisibleMouseOverVar<T>(Rect rect, Action<T> action, T val, string toolTip = "")
	{
		if (Mouse.IsOver(rect))
		{
			Widgets.DrawHighlight(rect);
		}
		if (Widgets.ButtonInvisible(rect))
		{
			action?.Invoke(val);
		}
		if (!toolTip.NullOrEmpty())
		{
			TooltipHandler.TipRegion(rect, toolTip);
		}
	}

	internal static void ButtonInvisibleVar<T>(Rect rect, Action<T> action, T value, string toolTip = "")
	{
		if (Widgets.ButtonInvisible(rect))
		{
			action?.Invoke(value);
		}
		if (!toolTip.NullOrEmpty())
		{
			TooltipHandler.TipRegion(rect, toolTip);
		}
	}

	internal static void ButtonSolid(Rect rect, Color color, Action action, string tooltip = "")
	{
		Widgets.DrawRectFast(rect, color);
		ButtonInvisible(rect, action, tooltip);
		GUI.color = Color.white;
	}

	internal static void ButtonText(Rect rect, string label, Action action, string toolTip = "")
	{
		if (Widgets.ButtonText(rect, label))
		{
			action?.Invoke();
		}
		if (!toolTip.NullOrEmpty())
		{
			TooltipHandler.TipRegion(rect, toolTip);
		}
	}

	internal static void ButtonText(float x, float y, float w, float h, string label, Action action, string toolTip = "")
	{
		Rect rect = new Rect(x, y, w, h);
		if (Widgets.ButtonText(rect, label))
		{
			action?.Invoke();
		}
		if (!toolTip.NullOrEmpty())
		{
			TooltipHandler.TipRegion(rect, toolTip);
		}
	}

	internal static void ButtonTextureTextHighlight(Rect rect, string text, Texture2D icon, Color color, Action action, string toolTip = "")
	{
		if (Mouse.IsOver(rect))
		{
			Widgets.DrawHighlight(rect);
		}
		GUI.color = color;
		GUI.DrawTexture(new Rect(rect.x, rect.y, rect.height, rect.height), (Texture)icon);
		GUI.color = Color.white;
		Text.Font = GameFont.Small;
		float num = rect.height - 10f - rect.height / 2f;
		Widgets.Label(new Rect(rect.x + rect.height + 5f, rect.y + num, rect.width - rect.height - 5f, rect.height), text);
		ButtonInvisible(rect, action, toolTip);
	}

	internal static void ButtonTextureTextHighlight2(Rect rect, string text, string texPath, Color color, Action action, string toolTip = "", bool withButton = true, float textOffset = 10f)
	{
		if (Mouse.IsOver(rect))
		{
			Widgets.DrawHighlight(rect);
		}
		if (texPath != null)
		{
			GUI.color = color;
			GUI.DrawTexture(new Rect(rect.x, rect.y, rect.height, rect.height), (Texture)ContentFinder<Texture2D>.Get(texPath));
			GUI.color = Color.white;
		}
		Text.Font = GameFont.Small;
		Text.Anchor = TextAnchor.MiddleLeft;
		float num = rect.height - textOffset - rect.height / 2f;
		if (texPath == null)
		{
			Widgets.Label(rect, text);
		}
		else
		{
			Widgets.Label(new Rect(rect.x + rect.height + 5f, rect.y + num, rect.width - rect.height - 5f, rect.height), text);
		}
		Text.Anchor = TextAnchor.UpperLeft;
		if (withButton)
		{
			ButtonInvisible(rect, action, toolTip);
		}
	}

	internal static void ButtonTextVar<T>(float x, float y, float w, float h, string label, Action<T> action, T value)
	{
		Rect rect = new Rect(x, y, w, h);
		if (Widgets.ButtonText(rect, label))
		{
			action?.Invoke(value);
		}
	}

	internal static void ButtonThingVar<T>(Rect rect, T val, Action<T> action, string tooltip)
	{
		if (Mouse.IsOver(rect))
		{
			Widgets.DrawHighlight(rect);
		}
		ThingDrawer(rect, val as Thing);
		ButtonInvisibleVar(rect, action, val, tooltip);
	}

	internal static void CheckBoxOnChange(Rect rect, string label, bool checkState, Action<bool> action)
	{
		bool flag = checkState;
		Widgets.CheckboxLabeled(rect, label, ref checkState);
		if (flag != checkState)
		{
			action?.Invoke(checkState);
		}
	}

	internal static void ColorBox(Rect rect, Color col, Action<Color> action, bool halfAlfa = false)
	{
		Listing_X listing_X = new Listing_X();
		listing_X.Begin(rect);
		GUI.color = Color.red;
		Type aType = Reflect.GetAType("Verse", "Widgets");
		aType.SetMemberValue("RangeControlTextColor", Color.red);
		float num = listing_X.Slider(col.r, 0f, ColorTool.IMAX);
		GUI.color = Color.green;
		aType.SetMemberValue("RangeControlTextColor", Color.green);
		float num2 = listing_X.Slider(col.g, 0f, ColorTool.IMAX);
		GUI.color = Color.blue;
		aType.SetMemberValue("RangeControlTextColor", Color.blue);
		float num3 = listing_X.Slider(col.b, 0f, ColorTool.IMAX);
		GUI.color = Color.white;
		aType.SetMemberValue("RangeControlTextColor", Color.white);
		float num4 = listing_X.Slider(col.a, halfAlfa ? 0.49f : 0f, ColorTool.IMAX);
		bool flag = col.r != num || col.g != num2 || col.b != num3 || col.a != num4;
		listing_X.End();
		if (flag)
		{
			action?.Invoke(new Color(num, num2, num3, num4));
		}
	}

	internal static void Image(Rect rect, string texPath)
	{
		GUI.DrawTexture(rect, (Texture)ContentFinder<Texture2D>.Get(texPath));
	}

	internal static void Image(Rect rect, Texture2D tex)
	{
		GUI.DrawTexture(rect, (Texture)tex);
	}

	internal static void LabelEdit(Rect rect, int id, string text, ref string value, GameFont font, bool capitalize = false)
	{
		Text.Font = font;
		Widgets.DrawBoxSolid(rect, ColorTool.colAsche);
		if (iLabelId != id)
		{
			if (Mouse.IsOver(rect))
			{
				Widgets.DrawHighlight(rect);
			}
			Rect rect2 = new Rect(rect);
			rect2.x += 3f;
			if (capitalize)
			{
				Widgets.Label(rect2, text.NullOrEmpty() ? value.CapitalizeFirst() : (text + " " + value.CapitalizeFirst()));
			}
			else
			{
				Widgets.Label(rect2, text.NullOrEmpty() ? value : (text + " " + value));
			}
			ButtonInvisible(rect, delegate
			{
				iLabelId = id;
			});
			TooltipHandler.TipRegion(rect, value);
		}
		else
		{
			Rect rect3 = new Rect(rect);
			rect3.width = rect3.height;
			rect3.x = rect.width - rect3.height;
			ButtonImage(rect3, "UI/Buttons/DragHash", delegate
			{
				iLabelId = -1;
			});
			value = Widgets.TextField(rect, value, 256, CharacterEditor.Label.ValidNameRegex);
		}
	}

	internal static void Label(Rect rect, string text, Action action = null, string tooltip = "")
	{
		if (text != null)
		{
			Widgets.Label(rect, text);
		}
		if (action != null)
		{
			ButtonInvisible(rect, action);
		}
		if (!tooltip.NullOrEmpty())
		{
			TooltipHandler.TipRegion(rect, tooltip);
		}
	}

	internal static void Label(float x, float y, float w, float h, string text, Action action = null)
	{
		Rect rect = new Rect(x, y, w, h);
		Widgets.Label(rect, text);
		if (action != null)
		{
			ButtonInvisible(rect, action);
		}
	}

	internal static void LabelBackground(Rect rect, string text, Color col, int offset = 0, string tooltip = "", Color colText = default(Color))
	{
		Widgets.DrawBoxSolid(rect, col);
		if (text == null)
		{
			text = "";
		}
		Rect rect2 = new Rect(rect.x + 3f + (float)offset, rect.y, rect.width - 3f, rect.height);
		if (rect.height > 20f && text.Length <= 22)
		{
			rect2.y += (rect.height - 20f) / 2f;
		}
		if (colText != default(Color))
		{
			Color color = GUI.color;
			GUI.color = colText;
			Widgets.Label(rect2, text);
			GUI.color = color;
		}
		else
		{
			Widgets.Label(rect2, text);
		}
		if (!tooltip.NullOrEmpty())
		{
			TooltipHandler.TipRegion(rect2, tooltip);
		}
	}

	internal static void LabelCol<T>(Rect rect, string text, Color col, Action<T> action, T value, string tooltip = "")
	{
		Color color = GUI.color;
		GUI.color = col;
		Widgets.Label(rect, text);
		GUI.color = color;
		if (action != null)
		{
			ButtonInvisibleVar(rect, action, value, tooltip);
		}
	}

	internal static void TraitListView(float x, float y, float w, float h, List<Trait> l, ref Vector2 scrollPos, int elemH, Action<Trait> onClick, Action<Trait> onRandom, Action<Trait> onPrev, Action<Trait> onNext, Func<Trait, string> Flabel, Func<Trait, string> Ftooltip)
	{
		if (l.NullOrEmpty())
		{
			return;
		}
		Rect outRect = new Rect(x, y, w, h);
		float height = l.Count * elemH + 20;
		Rect rect = new Rect(0f, 0f, outRect.width - 16f, height);
		Widgets.BeginScrollView(outRect, ref scrollPos, rect);
		Rect rect2 = rect.ContractedBy(6f);
		rect2.height = height;
		Color green = Color.green;
		Listing_X listing_X = new Listing_X();
		rect2.width += 18f;
		listing_X.Begin(rect2);
		listing_X.DefSelectionLineHeight = elemH;
		for (int i = 0; i < l.Count; i++)
		{
			if (listing_X.CurY + (float)elemH > scrollPos.y && listing_X.CurY - h < scrollPos.y)
			{
				Trait trait = l[i];
				Color colText = ((trait.sourceGene != null) ? ColorTool.colSkyBlue : Color.white);
				Rect rect3 = new Rect(listing_X.CurX, listing_X.CurY, outRect.width - 16f, elemH);
				string tooltip = "";
				if (Mouse.IsOver(rect3))
				{
					tooltip = Ftooltip(trait);
				}
				NavSelectorVar(rect3, trait, onClick, onRandom, onPrev, onNext, null, Flabel(trait), tooltip, null, colText);
			}
			listing_X.CurY += 25f;
		}
		listing_X.End();
		Widgets.EndScrollView();
	}

	internal static void AToggleSearch()
	{
		bToggleSearch = !bToggleSearch;
	}

	internal static ICollection<T> CreateSearch<T>(float x, ref float y, float w, float h, ICollection<T> l, Func<T, string> labelGetter)
	{
		ICollection<T> collection = new List<T>();
		try
		{
			Rect rect = new Rect(x, y, w, h);
			sFind = Widgets.TextField(rect, sFind, 256);
			char c = ((!sFind.NullOrEmpty()) ? sFind.First() : ' ');
			bool flag = char.IsUpper(c);
			string text = (flag ? sFind : sFind.ToLower());
			foreach (T item in l)
			{
				if (text.NullOrEmpty())
				{
					collection.Add(item);
				}
				else if (flag)
				{
					string text2 = labelGetter(item);
					if (text2.StartsWith(text))
					{
						collection.Add(item);
					}
				}
				else
				{
					string text2 = labelGetter(item).ToLower();
					if (text2.StartsWith(text) || text2.Contains(text))
					{
						collection.Add(item);
					}
				}
			}
		}
		catch
		{
		}
		y += 4f;
		return collection;
	}

	internal static float GetGraphicH<T>()
	{
		bool flag = typeof(T) == typeof(AbilityDef);
		bool flag2 = typeof(T) == typeof(HairDef);
		bool flag3 = typeof(T) == typeof(BeardDef);
		bool flag4 = typeof(T) == typeof(ThingDef);
		bool flag5 = typeof(T) == typeof(GeneDef);
		bool flag6 = typeof(T) == typeof(Pawn);
		return (flag || flag5 || flag2 || flag3 || flag4) ? 64 : (flag6 ? 90 : 0);
	}

	internal static void ListView<T>(float x, float y, float w, float h, ICollection<T> l, Func<T, string> labelGetter, Func<T, string> tooltipGetter, Func<T, T, bool> comparator, ref T selectedThing, ref Vector2 scrollPos, bool withRemove = false, Action<T> action = null, bool withSearch = true, bool drawSection = false, bool hasIcon = false, bool selectOnMouseOver = false)
	{
		if (l == null)
		{
			return;
		}
		bool flag = typeof(T) == typeof(AbilityDef);
		bool isHair = typeof(T) == typeof(HairDef);
		bool isBeard = typeof(T) == typeof(BeardDef);
		bool flag2 = typeof(T) == typeof(ThingDef);
		bool flag3 = typeof(T) == typeof(GeneDef);
		bool flag4 = typeof(T) == typeof(Pawn);
		bool flag5 = typeof(T) == typeof(ScenPart);
		float num = ((!(flag || flag3 || flag2)) ? ((flag4 || flag5) ? 22 : 32) : 0);
		float graphicH = GetGraphicH<T>();
		float num2 = (withSearch ? 25f : 0f);
		ICollection<T> collection = (withSearch ? CreateSearch(x, ref y, w, num2, l, labelGetter) : l);
		float height = 10f + (float)collection.Count * (num + graphicH);
		Rect rect = new Rect(x, y + num2, w, h - num2);
		Rect rect2 = new Rect(0f, 0f, rect.width - 16f, height);
		if (drawSection)
		{
			Widgets.DrawMenuSection(rect);
		}
		Widgets.BeginScrollView(rect, ref scrollPos, rect2);
		Rect rect3 = rect2.ContractedBy(6f);
		rect3.height = height;
		Color selColor = (drawSection ? Color.blue : Color.green);
		Listing_X listing_X = new Listing_X();
		rect3.width += 18f;
		listing_X.Begin(rect3);
		listing_X.DefSelectionLineHeight = num;
		try
		{
			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.MiddleLeft;
			if (flag4)
			{
				using IEnumerator<T> enumerator = collection.GetEnumerator();
				int num3 = 0;
				while (enumerator.MoveNext())
				{
					Pawn pawn = enumerator.Current as Pawn;
					bool selected = comparator(selectedThing, enumerator.Current);
					string tooltip = tooltipGetter(enumerator.Current);
					num3 = listing_X.Selectable(pawn.GetPawnName(needFull: true), selected, tooltip, PortraitsCache.Get(pawn, new Vector2(128f, 180f), Rot4.South), null, null, default(Vector2), withRemove: false, num + graphicH, (pawn.Faction == null) ? Color.white : pawn.Faction.Color, ColorTool.colLightGray);
					if (num3 == 1)
					{
						selectedThing = enumerator.Current;
						if (action != null)
						{
							action(selectedThing);
						}
						else
						{
							SoundDefOf.Mouseover_Category.PlayOneShotOnCamera();
						}
					}
				}
			}
			else
			{
				using IEnumerator<T> enumerator2 = collection.GetEnumerator();
				while (enumerator2.MoveNext())
				{
					if (listing_X.CurY + num + graphicH > scrollPos.y && listing_X.CurY - 700f < scrollPos.y)
					{
						string tooltip = tooltipGetter(enumerator2.Current);
						string name = labelGetter(enumerator2.Current);
						bool selected = comparator(enumerator2.Current, selectedThing);
						bool isWhite = true;
						if (listing_X.SelectableText(name, flag2, flag, flag3, isHair, isBeard, selected, tooltip, withRemove, isWhite, enumerator2.Current, selColor, hasIcon, selectOnMouseOver))
						{
							selectedThing = enumerator2.Current;
							action?.Invoke(selectedThing);
						}
					}
					listing_X.CurY += num;
					listing_X.CurY += graphicH;
				}
			}
			Text.Anchor = TextAnchor.UpperLeft;
		}
		catch
		{
		}
		listing_X.End();
		Widgets.EndScrollView();
	}

	internal static void ListView<T>(Rect rect, ICollection<T> l, Func<T, string> labelGetter, Func<T, string> tooltipGetter, Func<T, T, bool> comparator, ref T selectedThing, ref Vector2 scrollPos, bool withRemove = false, Action<T> action = null, bool withSearch = true, bool drawSection = false, bool isHead = false, bool selectOnMouse = false)
	{
		ListView(rect.x, rect.y, rect.width, rect.height, l, labelGetter, tooltipGetter, comparator, ref selectedThing, ref scrollPos, withRemove, action, withSearch, drawSection, isHead, selectOnMouse);
	}

	internal static void FullListviewScenPart(Rect rect, List<ScenPart> l, bool withRemove, Action<ScenPart> removeAction, string shiftIcon, Action<ScenPart> onShift, bool showPosition, bool withSearch, ref Vector2 scrollPos, ref ScenPart selectedPart)
	{
		if (l == null)
		{
			return;
		}
		float x = rect.x;
		float y = rect.y;
		float width = rect.width;
		float height = rect.height;
		float num = 32f;
		float num2 = (withSearch ? 25f : 0f);
		List<ScenPart> list = (withSearch ? CreateSearch(rect.x, ref y, width, num2, l, FLabel.ScenPartLabel).ToList() : l);
		float height2 = 10f + (float)list.Count * num;
		Rect outRect = new Rect(x, y + num2, width, height - num2);
		Rect rect2 = new Rect(0f, 0f, outRect.width - 16f, height2);
		Widgets.BeginScrollView(outRect, ref scrollPos, rect2);
		Rect rect3 = rect2.ContractedBy(6f);
		rect3.height = height2;
		Color green = Color.green;
		Listing_X listing_X = new Listing_X();
		rect3.width += 18f;
		listing_X.Begin(rect3);
		listing_X.DefSelectionLineHeight = num;
		try
		{
			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.MiddleLeft;
			using (List<ScenPart>.Enumerator enumerator = list.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (listing_X.CurY + num > scrollPos.y && listing_X.CurY - 700f < scrollPos.y && listing_X.ListviewPart(width, num, enumerator.Current, enumerator.Current == selectedPart, withRemove, removeAction, shiftIcon, onShift, showPosition))
					{
						selectedPart = ((selectedPart != null) ? null : enumerator.Current);
					}
					listing_X.CurY += num;
				}
			}
			Text.Anchor = TextAnchor.UpperLeft;
		}
		catch
		{
		}
		listing_X.End();
		Widgets.EndScrollView();
	}

	internal static float NavSelectorCount(Rect rect, Selected s, int max)
	{
		Text.Font = GameFont.Small;
		if (Widgets.ButtonImage(RectPrevious(rect), ContentFinder<Texture2D>.Get("bbackward")))
		{
			s.stackVal--;
			s.oldStackVal = s.stackVal;
			s.UpdateBuyPrice();
		}
		if (Widgets.ButtonImage(RectNext(rect), ContentFinder<Texture2D>.Get("bforward")))
		{
			s.stackVal++;
			s.oldStackVal = s.stackVal;
			s.UpdateBuyPrice();
		}
		LabelBackground(RectSolid(rect), CharacterEditor.Label.COUNT + s.stackVal, ColorTool.colAsche);
		if (Widgets.ButtonImage(RectToggle(rect), ContentFinder<Texture2D>.Get("UI/Buttons/DragHash")))
		{
			bCountOpen = !bCountOpen;
		}
		int num = 0;
		if (bCountOpen)
		{
			num++;
			s.stackVal = (int)Widgets.HorizontalSlider(RectSlider(rect, num), s.stackVal, 1f, max);
			if (s.stackVal != s.oldStackVal)
			{
				s.oldStackVal = s.stackVal;
				s.UpdateBuyPrice();
			}
		}
		if (s.tempThing != null)
		{
			s.tempThing.stackCount = s.stackVal;
		}
		return rect.y + 27f + (float)(num * 26);
	}

	internal static void NavSelectorImageBox(Rect rect, Action onClicked, Action onRandom, Action onPrev, Action onNext, Action onTextureClick, Action onToggle, string label, string tipLabel = null, string tipRandom = null, string tipTexture = null, string texturePath = null, Color colTex = default(Color), string tipToggle = null)
	{
		if (onPrev != null)
		{
			ButtonImage(RectPrevious(rect), "bbackward", onPrev);
		}
		if (onNext != null)
		{
			ButtonImage(RectNext(rect), "bforward", onNext);
		}
		Text.Font = GameFont.Small;
		bool flag = texturePath != null;
		bool flag2 = onToggle != null;
		LabelBackground(RectSolid(rect), label, ColorTool.colAsche, flag ? 25 : 0, "", colTex);
		ButtonImage(RectTexture(rect), texturePath, onTextureClick, tipTexture);
		int num = (CEditor.IsRandom ? 25 : 0);
		num += (flag2 ? 25 : 0);
		ButtonInvisibleMouseOver(RectOnClick(rect, flag, num), onClicked, tipLabel);
		if (CEditor.IsRandom && onRandom != null)
		{
			ButtonImage(RectRandom(rect), "brandom", onRandom, tipRandom);
			ButtonImage(RectToggleLeft(rect), flag2 ? "UI/Buttons/DragHash" : null, onToggle, tipToggle);
		}
		else
		{
			ButtonImage(RectToggle(rect), flag2 ? "UI/Buttons/DragHash" : null, onToggle, tipToggle);
		}
	}

	internal static void NavSelectorVar<T>(Rect rect, T val, Action<T> onClick, Action<T> onRandom, Action<T> onPrev, Action<T> onNext, Action<T> onToggle, string label, string tooltip, string tipRandom, Color colText)
	{
		bool isRandom = CEditor.IsRandom;
		if (isRandom && onPrev != null)
		{
			ButtonImageVar(RectPrevious(rect), "bbackward", onPrev, val);
		}
		if (isRandom && onNext != null)
		{
			ButtonImageVar(RectNext(rect), "bforward", onNext, val);
		}
		Text.Font = GameFont.Small;
		bool flag = onToggle != null;
		int offset = (CEditor.IsRandom ? 25 : 0);
		LabelBackground(RectSolid(rect, isRandom), label, ColorTool.colAsche, 0, "", colText);
		if (onClick != null)
		{
			ButtonInvisibleMouseOverVar(RectOnClick(rect, hasTexture: false, offset, isRandom), onClick, val, tooltip);
		}
		if (CEditor.IsRandom)
		{
			ButtonImageVar(RectRandom(rect), "brandom", onRandom, val, tipRandom);
			if (flag)
			{
				ButtonImageVar(RectToggleLeft(rect), flag ? "UI/Buttons/DragHash" : null, onToggle, val);
			}
		}
		else if (flag)
		{
			ButtonImageVar(RectToggle(rect), flag ? "UI/Buttons/DragHash" : null, onToggle, val);
		}
	}

	internal static void NavSelectorImageBox2<T>(Rect rect, T val, Action<T> onClicked, Action<T> onRandom, Action<T> onPrev, Action<T> onNext, Action<T> onTextureClick, Action<T> onToggle, string label, string tipLabel = null, string tipRandom = null, string tipTexture = null, string texturePath = null, Color colTex = default(Color))
	{
		if (onPrev != null)
		{
			ButtonImageVar(RectPrevious(rect), "bbackward", onPrev, val);
		}
		if (onNext != null)
		{
			ButtonImageVar(RectNext(rect), "bforward", onNext, val);
		}
		Text.Font = GameFont.Small;
		bool flag = val is Thing;
		bool flag2 = flag || texturePath != null;
		bool flag3 = onToggle != null;
		LabelBackground(RectSolid(rect), label, ColorTool.colAsche, flag2 ? 25 : 0);
		int num = (CEditor.IsRandom ? 25 : 0);
		num += (flag3 ? 25 : 0);
		if (flag)
		{
			ButtonThingVar(RectTexture(rect), val, onTextureClick, tipTexture);
		}
		else
		{
			ButtonImageVar(RectTexture(rect), texturePath, onTextureClick, val, tipTexture);
		}
		ButtonInvisibleMouseOverVar(RectOnClick(rect, flag2, num), onClicked, val, tipLabel);
		if (CEditor.IsRandom)
		{
			ButtonImageVar(RectRandom(rect), "brandom", onRandom, val, tipRandom);
			ButtonImageVar(RectToggleLeft(rect), flag3 ? "UI/Buttons/DragHash" : null, onToggle, val);
		}
		else
		{
			ButtonImageVar(RectToggle(rect), flag3 ? "UI/Buttons/DragHash" : null, onToggle, val);
		}
	}

	internal static float NavSelectorQuality(Rect rect, Selected s, HashSet<QualityCategory> lOfQuality)
	{
		if (s == null || !s.HasQuality)
		{
			return rect.y;
		}
		if (Widgets.ButtonImage(RectPrevious(rect), ContentFinder<Texture2D>.Get("bbackward")))
		{
			s.quality = lOfQuality.NextOrPrevIndex(s.quality, next: false, random: false);
			s.UpdateBuyPrice();
		}
		if (Widgets.ButtonImage(RectNext(rect), ContentFinder<Texture2D>.Get("bforward")))
		{
			s.quality = lOfQuality.NextOrPrevIndex(s.quality, next: true, random: false);
			s.UpdateBuyPrice();
		}
		Text.Font = GameFont.Small;
		LabelBackground(RectSolid(rect), CharacterEditor.Label.QUALITY + ((QualityCategory)s.quality).GetLabel().CapitalizeFirst(), ColorTool.colAsche);
		if (Widgets.ButtonImage(RectToggle(rect), ContentFinder<Texture2D>.Get("UI/Buttons/DragHash")))
		{
			bQualityOpen = !bQualityOpen;
		}
		Rect rect2 = RectClickableT(rect);
		if (Mouse.IsOver(rect2))
		{
			Widgets.DrawHighlight(rect2);
		}
		FloatMenuOnButtonInvisible(rect2, lOfQuality, (QualityCategory q) => q.GetLabel(), delegate(QualityCategory q)
		{
			s.quality = (int)q;
			s.UpdateBuyPrice();
		});
		int num = 0;
		if (bQualityOpen)
		{
			num++;
			s.quality = (int)Widgets.HorizontalSlider(RectSlider(rect, num), s.quality, 0f, lOfQuality.Count - 1);
		}
		if (s.tempThing != null)
		{
			s.tempThing.SetQuality(s.quality);
		}
		return rect.y + 27f + (float)(num * 26);
	}

	internal static float NavSelectorStuff(Rect rect, Selected s)
	{
		if (s == null || s.thingDef == null)
		{
			return rect.y;
		}
		bool madeFromStuff = s.thingDef.MadeFromStuff;
		if (!madeFromStuff)
		{
			return rect.y;
		}
		Text.Font = GameFont.Small;
		if (Widgets.ButtonImage(RectPrevious(rect), ContentFinder<Texture2D>.Get("bbackward")) && madeFromStuff)
		{
			s.SetStuff(next: false, random: false);
		}
		if (Widgets.ButtonImage(RectNext(rect), ContentFinder<Texture2D>.Get("bforward")) && madeFromStuff)
		{
			s.SetStuff(next: true, random: false);
		}
		LabelBackground(RectSolid(rect), CharacterEditor.Label.STUFF + s.StuffLabelGetter(s.stuff), ColorTool.colAsche, madeFromStuff ? 25 : 0);
		if (Widgets.ButtonImage(RectToggle(rect), ContentFinder<Texture2D>.Get("UI/Buttons/DragHash")))
		{
			bStuffOpen = !bStuffOpen;
		}
		if (madeFromStuff)
		{
			GUI.color = s.GetTColor();
			FloatMenuOnButtonStuffOrStyle(RectTexture(rect), RectClickableT(rect), s.lOfStuff, s.StuffLabelGetter, s, delegate(ThingDef stuff)
			{
				s.SetStuff(stuff);
			});
			GUI.color = Color.white;
		}
		if (s.tempThing != null)
		{
			s.tempThing.SetStuffDirect(s.stuff);
		}
		int num = 0;
		if (bStuffOpen && madeFromStuff)
		{
			num++;
			s.stuffIndex = (int)Widgets.HorizontalSlider(RectSlider(rect, num), s.stuffIndex, 0f, s.lOfStuff.Count - 1);
			s.CheckSetStuff();
		}
		return rect.y + 27f + (float)(num * 26);
	}

	internal static float NavSelectorStyle(Rect rect, Selected s)
	{
		if (s == null || s.thingDef == null)
		{
			return rect.y;
		}
		bool flag = s.thingDef.CanBeStyled() && s.lOfStyle.Count > 1;
		if (!flag)
		{
			return rect.y;
		}
		Text.Font = GameFont.Small;
		if (Widgets.ButtonImage(RectPrevious(rect), ContentFinder<Texture2D>.Get("bbackward")) && flag)
		{
			s.SetStyle(next: false, random: false);
		}
		if (Widgets.ButtonImage(RectNext(rect), ContentFinder<Texture2D>.Get("bforward")) && flag)
		{
			s.SetStyle(next: true, random: false);
		}
		string text = (flag ? s.StyleLabelGetter(s.style) : "");
		LabelBackground(RectSolid(rect), CharacterEditor.Label.STYLE + text, ColorTool.colAsche, flag ? 25 : 0);
		if (Widgets.ButtonImage(RectToggle(rect), ContentFinder<Texture2D>.Get("UI/Buttons/DragHash")))
		{
			bStyleOpen = !bStyleOpen;
		}
		if (flag)
		{
			FloatMenuOnButtonStuffOrStyle(RectTexture(rect), RectClickableT(rect), s.lOfStyle, s.StyleLabelGetter, s, delegate(ThingStyleDef style)
			{
				s.SetStyle(style);
			});
		}
		int num = 0;
		if (bStyleOpen && flag)
		{
			num++;
			s.styleIndex = (int)Widgets.HorizontalSlider(RectSlider(rect, num), s.styleIndex, 0f, s.lOfStyle.Count - 1);
			s.CheckSetStyle();
		}
		if (s.tempThing != null)
		{
			s.tempThing.SetStyleDef(s.style);
		}
		return rect.y + 27f + (float)(num * 26);
	}

	internal static float NumericFloatBox(Rect rect, float value, float min, float max)
	{
		return NumericFloatBox(rect.x, rect.y, rect.width, rect.height, value, min, max);
	}

	internal static float NumericFloatBox(float x, float y, float w, float h, float value, float min, float max)
	{
		Rect butRect = new Rect(x, y, 25f, h);
		Rect rect = new Rect(x + 20f, y, w, h);
		Rect butRect2 = new Rect(x + w + 15f, y, 25f, h);
		if (Widgets.ButtonImage(butRect, ContentFinder<Texture2D>.Get("bbackward")))
		{
			value -= 1f;
			value = (float)Math.Round(value, 2);
		}
		if (Widgets.ButtonImage(butRect2, ContentFinder<Texture2D>.Get("bforward")))
		{
			value += 1f;
			value = (float)Math.Round(value, 2);
		}
		string text = value.ToString();
		if (text.EndsWith("."))
		{
			text += "0";
		}
		else if (!text.Contains("."))
		{
			text += ".0";
		}
		text = Widgets.TextField(rect, text, 32);
		if (text.EndsWith("."))
		{
			text += "0";
		}
		else if (!text.Contains("."))
		{
			text += ".0";
		}
		float result = 0f;
		if (float.TryParse(text, out result))
		{
			value = ((result < min) ? min : ((!(result > max)) ? result : max));
		}
		return value;
	}

	internal static long NumericLongBox(Rect rect, long value, long min, long max)
	{
		return NumericLongBox(rect.x, rect.y, rect.width, rect.height, value, min, max);
	}

	internal static long NumericLongBox(float x, float y, float w, float h, long value, long min, long max)
	{
		Rect butRect = new Rect(x, y, 25f, h);
		Rect rect = new Rect(x + 20f, y, w, h);
		Rect butRect2 = new Rect(x + w + 15f, y, 25f, h);
		if (Widgets.ButtonImage(butRect, ContentFinder<Texture2D>.Get("bbackward")))
		{
			value--;
		}
		if (Widgets.ButtonImage(butRect2, ContentFinder<Texture2D>.Get("bforward")))
		{
			value++;
		}
		string text = value.ToString();
		text = Widgets.TextField(rect, text, 32);
		long result = 0L;
		if (long.TryParse(text, out result))
		{
			value = ((result < min) ? min : ((result <= max) ? result : max));
		}
		return value;
	}

	internal static int NumericIntBox(Rect rect, int value, int min, int max)
	{
		return NumericIntBox(rect.x, rect.y, rect.width, rect.height, value, min, max);
	}

	internal static int NumericIntBox(float x, float y, float w, float h, int value, int min, int max)
	{
		Rect butRect = new Rect(x, y, 25f, h);
		Rect rect = new Rect(x + 20f, y, w, h);
		Rect butRect2 = new Rect(x + w + 15f, y, 25f, h);
		if (Widgets.ButtonImage(butRect, ContentFinder<Texture2D>.Get("bbackward")))
		{
			value--;
		}
		if (Widgets.ButtonImage(butRect2, ContentFinder<Texture2D>.Get("bforward")))
		{
			value++;
		}
		string text = value.ToString();
		text = Widgets.TextField(rect, text, 32);
		int result = 0;
		if (int.TryParse(text, out result))
		{
			value = ((result < min) ? min : ((result <= max) ? result : max));
		}
		return value;
	}

	internal static int NumericTextField(Rect rect, int value, int min, int max)
	{
		string text = value.ToString();
		text = Widgets.TextField(rect, text, 32);
		int result = 0;
		if (int.TryParse(text, out result))
		{
			value = ((result < min) ? min : ((result <= max) ? result : max));
		}
		return value;
	}

	internal static int NumericTextField(float x, float y, float w, float h, int value, int min, int max)
	{
		Rect rect = new Rect(x + 20f, y, w, h);
		string text = value.ToString();
		text = Widgets.TextField(rect, text, 32);
		int result = 0;
		if (int.TryParse(text, out result))
		{
			value = ((result < min) ? min : ((result <= max) ? result : max));
		}
		return value;
	}

	internal static void ScrollView(int x, int y, int w, int h, int objCount, int objH, ref Vector2 scrollPos, Action<Listing_X> drawFunction)
	{
		Rect outRect = new Rect(x, y, w, h);
		Rect rect = new Rect(0f, y, outRect.width - 16f, objCount * objH);
		Widgets.BeginScrollView(outRect, ref scrollPos, rect);
		Rect rect2 = rect.ContractedBy(4f);
		rect2.height = objCount * objH;
		Listing_X listing_X = new Listing_X();
		listing_X.Begin(rect2);
		drawFunction(listing_X);
		listing_X.End();
		Widgets.EndScrollView();
	}

	internal static void SimpleMultiplierSlider(Rect rect, string label, string format, bool showNumeric, float baseValue, ref float currentVal, float min, float max)
	{
		Listing_X listing_X = new Listing_X();
		listing_X.Begin(rect);
		string selectedName = (showNumeric ? label : "");
		listing_X.AddMultiplierSection(label, format, ref selectedName, baseValue, ref currentVal, min, max, small: true);
		listing_X.End();
	}

	internal static void SimpleSlider(Rect rect, string label, ref float currentVal, float min, float max)
	{
		Listing_X listing_X = new Listing_X();
		listing_X.Begin(rect);
		string selectedName = label;
		listing_X.AddSection(label, "", ref selectedName, ref currentVal, min, max, small: true);
		listing_X.End();
	}

	internal static void SingleSlinder(Rect rect, float currentVal, float min, float max, Action<float> action)
	{
		Listing_X listing_X = new Listing_X();
		listing_X.Begin(rect);
		float num = listing_X.Slider(currentVal, min, max);
		bool flag = num != currentVal;
		listing_X.End();
		if (flag)
		{
			action?.Invoke(num);
		}
	}

	internal static string TextArea(Rect rect, string text, int max, Regex regex)
	{
		if (text == null)
		{
			text = "";
		}
		string text2 = GUI.TextArea(rect, text, max, Text.CurTextAreaStyle);
		if (text2.Length <= max && regex != null && regex.IsMatch(text2))
		{
			return text2;
		}
		return text;
	}

	internal static void ThingDrawer(Rect rect, Thing t)
	{
		Widgets.ThingIcon(rect, t);
		GUI.color = Color.white;
	}

	private static Rect RectClickableT(Rect rect)
	{
		return new Rect(rect.x + 21f, rect.y, rect.width - 64f, 24f);
	}

	private static Rect RectNext(Rect rect)
	{
		return new Rect(rect.x + rect.width - 22f, rect.y + 2f, 22f, 22f);
	}

	private static Rect RectOnClick(Rect rect, bool hasTexture, int offset = 0, bool showEdit = true)
	{
		return new Rect(rect.x + (float)(showEdit ? 21 : 0) + (float)(hasTexture ? 25 : 0), rect.y, rect.width - (float)(showEdit ? 40 : 19) - (float)(hasTexture ? 25 : 0) - (float)offset, 24f);
	}

	private static Rect RectPrevious(Rect rect)
	{
		return new Rect(rect.x, rect.y + 2f, 22f, 22f);
	}

	private static Rect RectRandom(Rect rect)
	{
		return new Rect(rect.x + rect.width - 42f, rect.y, 22f, 22f);
	}

	private static Rect RectSlider(Rect rect, int i)
	{
		return new Rect(rect.x, rect.y + 10f + (float)i * rect.height, rect.width, rect.height);
	}

	private static Rect RectSolid(Rect rect, bool showEdit = true)
	{
		return new Rect(rect.x + (float)(showEdit ? 21 : 0), rect.y, rect.width - (float)(showEdit ? 40 : 19), 24f);
	}

	private static Rect RectTexture(Rect rect)
	{
		return new Rect(rect.x + 25f, rect.y, 24f, 24f);
	}

	private static Rect RectToggle(Rect rect)
	{
		return new Rect(rect.x + rect.width - 42f, rect.y, 22f, 22f);
	}

	private static Rect RectToggleLeft(Rect rect)
	{
		return new Rect(rect.x + rect.width - 67f, rect.y, 22f, 22f);
	}

	private static void CheckAddTempTextToList(ref List<string> l)
	{
		if (iLabelId == iTempTextID)
		{
			return;
		}
		if (!tempText.NullOrEmpty())
		{
			if (l == null)
			{
				l = new List<string>();
			}
			l.Add(tempText);
		}
		tempText = "";
		iShowId = 0;
		iLabelId = -1;
	}

	private static void CheckAddTempTextToFList(ref List<float> l)
	{
		if (iLabelId == iTempTextID)
		{
			return;
		}
		if (float.TryParse(tempText, out var result))
		{
			if (l == null)
			{
				l = new List<float>();
			}
			l.Add(result);
		}
		tempText = "";
		iShowId = 0;
		iLabelId = -1;
	}

	internal static void ActivateLabelEdit(int id)
	{
		iShowId = id;
		iLabelId = iTempTextID;
	}

	internal static void AddLabelEditToList(Listing_X view, int id, ref List<string> l, Action action)
	{
		if (iShowId == id)
		{
			action?.Invoke();
			LabelEdit(view.GetRect(22f), iTempTextID, "", ref tempText, GameFont.Small);
			CheckAddTempTextToList(ref l);
		}
	}

	internal static void AddLabelEditToList(Listing_X view, int id, ref List<float> l, Action action)
	{
		if (iShowId == id)
		{
			action?.Invoke();
			LabelEdit(view.GetRect(22f), iTempTextID, "", ref tempText, GameFont.Small);
			CheckAddTempTextToFList(ref l);
		}
	}

	internal static void ToggleRemove()
	{
		bRemoveOnClick = !bRemoveOnClick;
	}

	internal static void DrawTagFilter(ref TagFilter t, List<string> lSamples, int w, Listing_X view, string title, ref List<string> copyList, Action<string> remove, Action<string> add)
	{
		view.Label(0f, 0f, w - 28, 30f, title, GameFont.Medium);
		view.FloatMenuOnButtonImage(w - 60, 5f, 24f, 24f, "UI/Buttons/Dev/Add", lSamples, (string s) => s, add);
		view.ButtonImage(w - 85, 5f, 24f, 24f, "bminus", ToggleRemove, RemoveColor);
		if (view.ButtonImage(w - 110, 5f, 18f, 24f, "UI/Buttons/Copy", null) && t != null)
		{
			t.tags.CopyList(ref copyList);
		}
		if (!copyList.NullOrEmpty() && view.ButtonImage(w - 130, 5f, 18f, 24f, "UI/Buttons/Paste", null))
		{
			if (t == null)
			{
				t = new TagFilter();
			}
			t.tags.PasteList(copyList);
		}
		view.Gap(30f);
		if (t != null)
		{
			view.FullListViewString(w - 28, t.tags, bRemoveOnClick, remove);
		}
		view.GapLine(25f);
	}

	internal static void DrawStringList(ref List<string> l, List<string> lSamples, int w, Listing_X view, string title, ref List<string> copyList, Action<string> remove, Action<string> add)
	{
		view.Label(0f, 0f, w - 28, 30f, title, GameFont.Medium);
		view.FloatMenuOnButtonImage(w - 60, 5f, 24f, 24f, "UI/Buttons/Dev/Add", lSamples, (string s) => s, add);
		view.ButtonImage(w - 85, 5f, 24f, 24f, "bminus", ToggleRemove, RemoveColor);
		if (view.ButtonImage(w - 110, 5f, 18f, 24f, "UI/Buttons/Copy", null))
		{
			l.CopyList(ref copyList);
		}
		if (!copyList.NullOrEmpty() && view.ButtonImage(w - 130, 5f, 18f, 24f, "UI/Buttons/Paste", null))
		{
			if (l.NullOrEmpty())
			{
				l = new List<string>();
			}
			l.PasteList(copyList);
		}
		view.Gap(30f);
		view.FullListViewString(w - 28, l, bRemoveOnClick, remove);
		view.GapLine(25f);
	}

	internal static void DrawStringListCustom(ref List<string> l, int id, int w, Listing_X view, string title, ref List<string> copyList, Action<string> remove, Action actionBeforeAdding = null)
	{
		view.Label(0f, 0f, w - 28, 30f, title, GameFont.Medium);
		view.ButtonImage(w - 60, 5f, 24f, 24f, "UI/Buttons/Dev/Add", delegate
		{
			ActivateLabelEdit(id);
		});
		view.ButtonImage(w - 85, 5f, 24f, 24f, "bminus", ToggleRemove, RemoveColor);
		if (view.ButtonImage(w - 110, 5f, 18f, 24f, "UI/Buttons/Copy", null))
		{
			l.CopyList(ref copyList);
		}
		if (!copyList.NullOrEmpty() && view.ButtonImage(w - 130, 5f, 18f, 24f, "UI/Buttons/Paste", null))
		{
			if (l.NullOrEmpty())
			{
				l = new List<string>();
			}
			l.PasteList(copyList);
		}
		view.Gap(30f);
		AddLabelEditToList(view, id, ref l, actionBeforeAdding);
		view.FullListViewString(w - 28, l, bRemoveOnClick, remove);
		view.GapLine(25f);
	}
}
