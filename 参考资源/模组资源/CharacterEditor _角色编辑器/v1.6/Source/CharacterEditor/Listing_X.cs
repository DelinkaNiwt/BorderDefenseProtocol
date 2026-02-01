using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace CharacterEditor;

internal class Listing_X : Listing
{
	internal float DefSelectionLineHeight = 22f;

	private GameFont font;

	private Color MoodColor = new Color(0.1f, 1f, 0.1f);

	private Color MoodColorNegative = new Color(0.8f, 0.4f, 0.4f);

	private Color NoEffectColor = new Color(0.5f, 0.5f, 0.5f, 0.75f);

	private List<Pair<Vector2, Vector2>> labelScrollbarPositions;

	private List<Vector2> labelScrollbarPositionsSetThisFrame;

	private Texture2D texRemove;

	private bool alternate = true;

	internal float CurY
	{
		get
		{
			return curY;
		}
		set
		{
			curY = value;
		}
	}

	internal float CurX
	{
		get
		{
			return curX;
		}
		set
		{
			curX = value;
		}
	}

	internal Listing_X(GameFont font)
	{
		this.font = font;
	}

	internal Listing_X()
	{
		font = GameFont.Small;
	}

	public override void Begin(Rect rect)
	{
		base.Begin(rect);
		Text.Font = font;
		texRemove = ContentFinder<Texture2D>.Get("UI/Buttons/Delete");
	}

	internal Listing_Standard BeginSection(float height)
	{
		Rect rect = GetRect(height + 8f);
		Widgets.DrawMenuSection(rect);
		Listing_Standard listing_Standard = new Listing_Standard();
		listing_Standard.Begin(rect.ContractedBy(4f));
		return listing_Standard;
	}

	internal void FloatMenuOnButtonImage<T>(float xOff, float yOff, float w, float h, string texPath, List<T> l, Func<T, string> labelGetter, Action<T> action)
	{
		Rect butRect = new Rect(curX + xOff, curY + yOff, w, h);
		if (Widgets.ButtonImage(butRect, ContentFinder<Texture2D>.Get(texPath)))
		{
			SZWidgets.FloatMenuOnRect(l, labelGetter, action);
		}
	}

	internal bool ButtonImage(float xOff, float yOff, float w, float h, string texPath, Action action, Color? color = null)
	{
		Rect butRect = new Rect(curX + xOff, curY + yOff, w, h);
		bool flag = Widgets.ButtonImage(butRect, ContentFinder<Texture2D>.Get(texPath), color ?? Color.white);
		if (flag)
		{
			action?.Invoke();
		}
		return flag;
	}

	internal bool ButtonImage<T>(float xOff, float yOff, float w, float h, string texPath, Color color, Action<T> action, T val, string toolTip = "")
	{
		Rect rect = new Rect(curX + xOff, curY + yOff, w, h);
		bool flag = Widgets.ButtonImage(rect, ContentFinder<Texture2D>.Get(texPath), color);
		if (flag)
		{
			action?.Invoke(val);
		}
		if (!toolTip.NullOrEmpty())
		{
			TooltipHandler.TipRegion(rect, toolTip);
		}
		return flag;
	}

	internal bool ButtonImage(Texture2D tex, float xOffset, float width, float height, Action action)
	{
		NewColumnIfNeeded(height);
		bool flag = Widgets.ButtonImage(new Rect(curX + xOffset, curY, width, height), tex);
		Gap(height + verticalSpacing);
		if (flag)
		{
			action?.Invoke();
		}
		return flag;
	}

	internal bool ButtonText(string label, float x, float y, float w, float h, Action action, string highlightTag = null)
	{
		Rect rect = new Rect(x, y, w, h);
		bool flag = Widgets.ButtonText(rect, label, drawBackground: true, doMouseoverSound: false);
		if (highlightTag != null)
		{
			UIHighlighter.HighlightOpportunity(rect, highlightTag);
		}
		if (flag)
		{
			action?.Invoke();
		}
		return flag;
	}

	internal bool ButtonText(string label, string highlightTag = null)
	{
		Rect rect = GetRect(30f);
		bool result = Widgets.ButtonText(rect, label, drawBackground: true, doMouseoverSound: false);
		if (highlightTag != null)
		{
			UIHighlighter.HighlightOpportunity(rect, highlightTag);
		}
		Gap(verticalSpacing);
		return result;
	}

	internal bool ButtonTextLabeled(string label, string buttonLabel)
	{
		Rect rect = GetRect(30f);
		Widgets.Label(rect.LeftHalf(), label);
		bool result = Widgets.ButtonText(rect.RightHalf(), buttonLabel, drawBackground: true, doMouseoverSound: false);
		Gap(verticalSpacing);
		return result;
	}

	internal void CheckboxLabeledWithDefault(string label, float xOff, float width, ref bool checkOn, bool defaultVal, string tooltip = null)
	{
		Rect rect = BaseCheckboxLabeled(label, xOff, width - 24f, ref checkOn, tooltip);
		Rect rect2 = new Rect(rect.x + rect.width, rect.y, 24f, 24f);
		if (Widgets.ButtonImage(rect2, ContentFinder<Texture2D>.Get("bdefault")))
		{
			checkOn = defaultVal;
		}
		TooltipHandler.TipRegion(rect2, CharacterEditor.Label.O_SETTODEFAULT);
		Gap(verticalSpacing);
	}

	internal void LabelEdit(int id, string text, ref string value, GameFont font)
	{
		SZWidgets.LabelEdit(GetRect(22f), id, text, ref value, font);
		Gap(4f);
	}

	internal void CheckboxLabeledNoGap(string label, float xOff, float width, ref bool checkOn)
	{
		BaseCheckboxLabeled(label, xOff, width, ref checkOn);
	}

	internal void CheckboxLabeled(string label, float xOff, float width, ref bool checkOn, string tooltip = null, int gap = -1)
	{
		BaseCheckboxLabeled(label, xOff, width, ref checkOn, tooltip);
		if (gap < 0)
		{
			Gap(verticalSpacing);
		}
		else
		{
			Gap(gap);
		}
	}

	private Rect BaseCheckboxLabeled(string label, float xOff, float width, ref bool checkOn, string tooltip = null, bool nearText = false)
	{
		float lineHeight = Text.LineHeight;
		Rect rect = GetRect(lineHeight);
		rect.width = width;
		rect.x += xOff;
		Widgets.DrawBoxSolid(rect, ColorTool.colAsche);
		if (!tooltip.NullOrEmpty())
		{
			TooltipHandler.TipRegion(rect, tooltip);
		}
		if (Mouse.IsOver(rect))
		{
			Widgets.DrawHighlight(rect);
		}
		Widgets.CheckboxLabeled(rect, label, ref checkOn, disabled: false, null, null, nearText);
		return rect;
	}

	internal bool CheckboxLabeledSelectable(string label, ref bool selected, ref bool checkOn)
	{
		float lineHeight = Text.LineHeight;
		Rect rect = GetRect(lineHeight);
		bool result = Widgets.CheckboxLabeledSelectable(rect, label, ref selected, ref checkOn);
		Gap(verticalSpacing);
		return result;
	}

	public override void End()
	{
		base.End();
		if (labelScrollbarPositions == null)
		{
			return;
		}
		for (int num = labelScrollbarPositions.Count - 1; num >= 0; num--)
		{
			if (!labelScrollbarPositionsSetThisFrame.Contains(labelScrollbarPositions[num].First))
			{
				labelScrollbarPositions.RemoveAt(num);
			}
		}
		labelScrollbarPositionsSetThisFrame.Clear();
	}

	internal void EndSection(Listing_Standard listing)
	{
		listing.End();
	}

	internal void FloatMenuButtonWithLabelDef<T>(string label, float wLabel, float wDropbox, string currentVal, ICollection<T> l, Func<T, string> labelGetter, Action<T> action, float gap = -1f) where T : Def
	{
		Rect rect = new Rect(curX, curY + 4f, wLabel, 30f);
		Rect rect2 = new Rect(curX + wLabel, curY, wDropbox, 30f);
		Widgets.Label(rect, label);
		if (!l.EnumerableNullOrEmpty())
		{
			SZWidgets.FloatMenuOnButtonText(rect2, currentVal, l, labelGetter, action);
		}
		if (gap == -1f)
		{
			Gap(verticalSpacing);
		}
		else
		{
			Gap(gap);
		}
	}

	internal void FloatMenuButtonWithLabel<T>(string label, float wLabel, float wDropbox, string currentVal, List<T> l, Func<T, string> labelGetter, Action<T> action, float gap = -1f)
	{
		Rect rect = new Rect(curX, curY + 4f, wLabel, 30f);
		Rect rect2 = new Rect(curX + wLabel, curY, wDropbox, 30f);
		Widgets.Label(rect, label);
		if (!l.NullOrEmpty())
		{
			SZWidgets.FloatMenuOnButtonText(rect2, currentVal, l, labelGetter, action);
		}
		if (gap == -1f)
		{
			Gap(verticalSpacing);
		}
		else
		{
			Gap(gap);
		}
	}

	internal void IntAdjuster(ref int val, int countChange, int min = 0)
	{
		Rect rect = GetRect(24f);
		rect.width = 42f;
		if (Widgets.ButtonText(rect, "-" + countChange, drawBackground: true, doMouseoverSound: false))
		{
			val -= countChange * GenUI.CurrentAdjustmentMultiplier();
			if (val < min)
			{
				val = min;
			}
		}
		rect.x += rect.width + 2f;
		if (Widgets.ButtonText(rect, "+" + countChange, drawBackground: true, doMouseoverSound: false))
		{
			val += countChange * GenUI.CurrentAdjustmentMultiplier();
			if (val < min)
			{
				val = min;
			}
		}
		Gap(verticalSpacing);
	}

	internal void IntEntry(ref int val, ref string editBuffer, int multiplier = 1)
	{
		Rect rect = GetRect(24f);
		Widgets.IntEntry(rect, ref val, ref editBuffer, multiplier);
		Gap(verticalSpacing);
	}

	internal void IntRange(ref IntRange range, int min, int max)
	{
		Rect rect = GetRect(28f);
		Widgets.IntRange(rect, (int)base.CurHeight, ref range, min, max);
		Gap(verticalSpacing);
	}

	internal void IntSetter(ref int val, int target, string label)
	{
		Rect rect = GetRect(24f);
		if (Widgets.ButtonText(rect, label, drawBackground: true, doMouseoverSound: false))
		{
			val = target;
		}
		Gap(verticalSpacing);
	}

	internal void Label(float xOff, float yOff, float w, float h, string text, GameFont font = GameFont.Small, string tooltip = "")
	{
		Text.Font = font;
		Rect rect = new Rect(curX + xOff, curY + yOff, w, h);
		Widgets.Label(rect, text);
		Text.Font = GameFont.Small;
		if (!tooltip.NullOrEmpty())
		{
			TooltipHandler.TipRegion(rect, tooltip);
		}
	}

	internal void LabelSimple(string label, float x, float y, float w, float h, string tooltip = null)
	{
		Rect rect = new Rect(x, y, w, h);
		Widgets.Label(rect, label);
		if (tooltip != null)
		{
			TooltipHandler.TipRegion(rect, tooltip);
		}
	}

	internal void Label(string text, float width = -1f, float yGap = -1f, float maxHeight = -1f, string tooltip = null)
	{
		float num = Text.CalcHeight(text, base.ColumnWidth);
		bool flag = false;
		if (maxHeight >= 0f && num > maxHeight)
		{
			num = maxHeight;
			flag = true;
		}
		Rect rect = GetRect(num);
		if (width >= 0f)
		{
			rect.width = width;
		}
		if (flag)
		{
			Vector2 scrollbarPosition = GetLabelScrollbarPosition(curX, curY);
			Widgets.LabelScrollable(rect, text, ref scrollbarPosition);
			SetLabelScrollbarPosition(curX, curY, scrollbarPosition);
		}
		else
		{
			Widgets.Label(rect, text);
		}
		if (tooltip != null)
		{
			TooltipHandler.TipRegion(rect, tooltip);
		}
		if (yGap == -1f)
		{
			Gap(verticalSpacing);
		}
		else
		{
			Gap(yGap);
		}
	}

	internal bool Listview(float width, string defName, string name, string tooltip, bool withRemove, Action<string> action)
	{
		if (name == null)
		{
			return false;
		}
		Text.Font = GameFont.Small;
		Text.Anchor = TextAnchor.MiddleLeft;
		Rect rect = new Rect(curX, curY, width - DefSelectionLineHeight, DefSelectionLineHeight);
		Text.WordWrap = false;
		Widgets.Label(rect, name);
		Text.WordWrap = true;
		if (!string.IsNullOrEmpty(tooltip))
		{
			TooltipHandler.TipRegion(tip: new TipSignal(() => tooltip, 275), rect: rect);
		}
		Text.Anchor = TextAnchor.UpperLeft;
		curY += DefSelectionLineHeight;
		if (withRemove)
		{
			Rect butRect = new Rect(rect.x + rect.width, rect.y, DefSelectionLineHeight, DefSelectionLineHeight);
			bool flag = Widgets.ButtonImage(butRect, texRemove, Color.grey);
			if (flag)
			{
				action?.Invoke(defName);
			}
			GUI.color = Color.white;
			return flag;
		}
		return Widgets.ButtonInvisible(rect);
	}

	internal bool ListviewTDC(float width, ThingDefCountClass tdc, bool selected, bool withRemove, Action<string> action, ThingDef t)
	{
		if (tdc == null || tdc.thingDef == null)
		{
			return false;
		}
		Text.Font = GameFont.Small;
		Text.Anchor = TextAnchor.MiddleLeft;
		Rect rect = new Rect(curX, curY, width - DefSelectionLineHeight, DefSelectionLineHeight);
		Text.WordWrap = false;
		Widgets.Label(rect, tdc.thingDef.label + ": " + tdc.count);
		Text.WordWrap = true;
		if (selected)
		{
			curY += DefSelectionLineHeight;
			int count = tdc.count;
			count = SZWidgets.NumericIntBox(curX, curY, 70f, 30f, count, 0, 10000);
			t.UpdateCost(tdc.thingDef, count);
			curY += DefSelectionLineHeight;
		}
		if (!string.IsNullOrEmpty(tdc.thingDef.description))
		{
			TooltipHandler.TipRegion(tip: new TipSignal(() => tdc.thingDef.description, 21275), rect: rect);
		}
		Text.Anchor = TextAnchor.UpperLeft;
		curY += DefSelectionLineHeight;
		if (withRemove)
		{
			Rect butRect = new Rect(rect.x + rect.width, rect.y, DefSelectionLineHeight, DefSelectionLineHeight);
			if (Widgets.ButtonImage(butRect, texRemove, Color.grey))
			{
				action?.Invoke(tdc.thingDef.defName);
			}
			GUI.color = Color.white;
		}
		return Widgets.ButtonInvisible(rect);
	}

	internal void GapLineCustom(float gapVorLinie, float gapNachLinie)
	{
		float y = curY + gapVorLinie / 2f;
		Color color = GUI.color;
		GUI.color = color * new Color(1f, 1f, 1f, 0.4f);
		Widgets.DrawLineHorizontal(curX, y, base.ColumnWidth);
		GUI.color = color;
		curY += gapVorLinie / 2f + gapNachLinie;
	}

	internal bool ListviewPart(float width, float itemH, ScenPart part, bool selected, bool removeActive, Action<ScenPart> action, string shiftIcon, Action<ScenPart> onShift, bool showPosition = false)
	{
		if (!part.IsSupportedScenarioPart())
		{
			return false;
		}
		Text.Font = GameFont.Small;
		Text.Anchor = TextAnchor.MiddleLeft;
		Rect rect = new Rect(curX, curY, width - itemH + 7f, itemH);
		Rect rect2 = new Rect(curX, curY, itemH, itemH);
		Rect rect3 = new Rect(curX + width - itemH - itemH, curY, itemH, itemH);
		Rect rect4 = new Rect(curX + 5f + itemH, curY, width, itemH);
		Rect rect5 = new Rect(curX, curY, width - itemH - itemH, itemH);
		Widgets.DrawBoxSolid(rect, alternate ? new Color(0.3f, 0.3f, 0.3f, 0.5f) : new Color(0.2f, 0.2f, 0.2f, 0.5f));
		alternate = !alternate;
		Text.WordWrap = false;
		Selected selectedScenarioPart = part.GetSelectedScenarioPart();
		if (part.IsScenarioAnimal())
		{
			if (selectedScenarioPart.pkd != null)
			{
				Widgets.ThingIcon(rect2, selectedScenarioPart.pkd.race);
				Widgets.Label(rect4, FLabel.PawnKindWithGenderAndAge(selectedScenarioPart));
			}
			else
			{
				Widgets.Label(rect, part.Label + ": " + selectedScenarioPart.stackVal);
			}
		}
		else if (selectedScenarioPart.thingDef != null)
		{
			GUI.color = selectedScenarioPart.GetTColor();
			GUI.DrawTexture(rect2, (Texture)selectedScenarioPart.GetTexture2D, (ScaleMode)1, true);
			GUI.color = Color.white;
			Widgets.Label(rect4, FLabel.ThingLabel(selectedScenarioPart));
		}
		Text.WordWrap = true;
		if (selected)
		{
			if (showPosition)
			{
				try
				{
					Reflect.GetAType("Verse", "CameraJumper").CallMethod("JumpLocalInternal", new object[2]
					{
						selectedScenarioPart.location,
						CameraJumper.MovementMode.Pan
					});
				}
				catch
				{
				}
			}
			else
			{
				selectedScenarioPart.oldStackVal = selectedScenarioPart.stackVal;
				selectedScenarioPart.stackVal = SZWidgets.NumericIntBox(curX + width - 140f - (float)(removeActive ? 25 : 0), curY + 2f, 70f, 26f, selectedScenarioPart.stackVal, 1, 20000);
				if (selectedScenarioPart.stackVal != selectedScenarioPart.oldStackVal)
				{
					part.SetScenarioPartCount(selectedScenarioPart.stackVal);
				}
			}
		}
		if (Mouse.IsOver(rect5))
		{
			TooltipHandler.TipRegion(rect5, selectedScenarioPart.thingDef.STooltip());
		}
		Text.Anchor = TextAnchor.UpperLeft;
		if (removeActive)
		{
			Rect butRect = new Rect(rect.x + rect.width - itemH, rect.y, itemH, itemH);
			if (Widgets.ButtonImage(butRect, texRemove, Color.grey))
			{
				action?.Invoke(part);
			}
			GUI.color = Color.white;
		}
		else if (!shiftIcon.NullOrEmpty())
		{
			SZWidgets.ButtonImageVar(rect3, shiftIcon, onShift, part);
		}
		return Widgets.ButtonInvisible(rect);
	}

	internal void FullListViewParam2<T, TDef>(List<T> l, ref T selected, Func<T, TDef> defGetter, Func<T, string> labelGetter, bool bRemoveOnClick, Action<T> removeAction) where TDef : Def
	{
		if (l.NullOrEmpty())
		{
			return;
		}
		for (int i = 0; i < l.Count; i++)
		{
			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.MiddleLeft;
			Rect rect = new Rect(curX, curY, 400f - DefSelectionLineHeight, DefSelectionLineHeight);
			Widgets.Label(rect, labelGetter(l[i]));
			Text.Anchor = TextAnchor.UpperLeft;
			curY += DefSelectionLineHeight;
			if (bRemoveOnClick)
			{
				BlockRemove(rect, l[i], ref selected, removeAction);
			}
			else
			{
				BlockSelectClick(rect, l[i], ref selected);
			}
		}
	}

	internal void FullListViewFloat(int w, List<float> l, bool bRemoveOnClick, Action<float> removeAction)
	{
		if (!l.NullOrEmpty())
		{
			float selectedVal = 0f;
			for (int i = 0; i < l.Count; i++)
			{
				float val = l[i];
				ListViewFloat(w, val, ref selectedVal, bRemoveOnClick, removeAction);
			}
		}
	}

	internal void ListViewFloat(float w, float val, ref float selectedVal, bool bRemoveOnClick, Action<float> removeAction)
	{
		Text.Font = GameFont.Small;
		Rect rect = new Rect(curX, curY, w - DefSelectionLineHeight, DefSelectionLineHeight);
		Widgets.Label(rect, val.ToString());
		curY += DefSelectionLineHeight;
		if (bRemoveOnClick)
		{
			BlockRemove(rect, val, ref selectedVal, removeAction);
		}
		else
		{
			BlockSelectClick(rect, val, ref selectedVal);
		}
	}

	internal void FullListViewString(int w, List<string> l, bool bRemoveOnClick, Action<string> removeAction)
	{
		if (!l.NullOrEmpty())
		{
			string selectedVal = null;
			for (int i = 0; i < l.Count; i++)
			{
				string val = l[i];
				ListViewString(w, val, ref selectedVal, bRemoveOnClick, removeAction);
			}
		}
	}

	internal void ListViewString(float w, string val, ref string selectedVal, bool bRemoveOnClick, Action<string> removeAction)
	{
		Text.Font = GameFont.Small;
		Rect rect = new Rect(curX, curY, w - DefSelectionLineHeight, DefSelectionLineHeight);
		Text.WordWrap = false;
		Widgets.Label(rect, val ?? "");
		Text.WordWrap = true;
		TipSignal tip = new TipSignal(() => val, 275);
		TooltipHandler.TipRegion(rect, tip);
		curY += DefSelectionLineHeight;
		if (bRemoveOnClick)
		{
			BlockRemove(rect, val, ref selectedVal, removeAction);
		}
		else
		{
			BlockSelectClick(rect, val, ref selectedVal);
		}
	}

	internal void FullListViewParam1<T>(List<T> l, ref T selected, bool bRemoveOnClick, Action<T> removeAction) where T : Def
	{
		if (!l.NullOrEmpty())
		{
			for (int i = 0; i < l.Count; i++)
			{
				T def = l[i];
				ListViewParam1(400f, def, ref selected, bRemoveOnClick, removeAction);
			}
		}
	}

	internal void FullListViewWorkTags(WorkTags workTags, bool bRemoveOnClick, Action<WorkTags> removeAction)
	{
		if (workTags == WorkTags.None)
		{
			return;
		}
		List<WorkTags> list = workTags.GetAllSelectedItems<WorkTags>().ToList();
		for (int i = 0; i < list.Count; i++)
		{
			WorkTags workTags2 = list[i];
			if (workTags2 == WorkTags.None)
			{
				continue;
			}
			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.MiddleLeft;
			Rect rect = new Rect(curX, curY, 400f - DefSelectionLineHeight, DefSelectionLineHeight);
			Text.WordWrap = false;
			string label = workTags2.LabelTranslated().CapitalizeFirst();
			Widgets.Label(rect, label);
			Text.WordWrap = true;
			Text.Anchor = TextAnchor.UpperLeft;
			curY += DefSelectionLineHeight;
			if (bRemoveOnClick)
			{
				Rect butRect = new Rect(rect.x + rect.width, rect.y, DefSelectionLineHeight, DefSelectionLineHeight);
				if (Widgets.ButtonImage(butRect, texRemove, Color.grey))
				{
					removeAction?.Invoke(workTags2);
				}
				GUI.color = Color.white;
			}
			else
			{
				Widgets.ButtonInvisible(rect);
			}
		}
	}

	internal void FullListViewParam<T, T2>(List<T2> l, ref T selected, Func<T2, T> defGetter, Func<T2, float> valueGetter, Func<T2, float> secValueGetter, Func<T2, float> minGetter, Func<T2, float> maxGetter, bool isInt, bool bRemoveOnClick, Action<T2, float> valueSetter, Action<T2, float> secValueSetter, Action<T> removeAction) where T : Def
	{
		if (l.NullOrEmpty())
		{
			return;
		}
		for (int i = 0; i < l.Count; i++)
		{
			T2 val = l[i];
			float value = valueGetter(val);
			float secValue = secValueGetter?.Invoke(val) ?? 0f;
			ListViewParam(400f, defGetter(val), ref selected, ref value, ref secValue, minGetter(val), maxGetter(val), isInt, bRemoveOnClick, removeAction);
			if (selected == defGetter(val))
			{
				valueSetter(val, value);
				secValueSetter?.Invoke(val, secValue);
			}
		}
	}

	private Rect BlockLabel<T>(int offset, float width, T def, float value, float secValue) where T : Def
	{
		Text.Font = GameFont.Small;
		Text.Anchor = TextAnchor.MiddleLeft;
		Rect rect = new Rect(curX + (float)offset, curY, width - DefSelectionLineHeight - (float)offset, DefSelectionLineHeight);
		Text.WordWrap = false;
		if (value != float.MinValue)
		{
			if (typeof(T) == typeof(PawnCapacityDef))
			{
				Widgets.Label(rect, def.SLabel() + " offset: " + value + "      factor: " + secValue);
			}
			else
			{
				Widgets.Label(rect, def.SLabel() + ": " + value);
			}
		}
		else if (typeof(T) == typeof(HeadTypeDef))
		{
			Widgets.Label(rect, def.SDefname());
		}
		else
		{
			Widgets.Label(rect, def.SLabel());
		}
		Text.WordWrap = true;
		return rect;
	}

	private void BlockTooltip<T>(Rect rect, T def) where T : Def
	{
		string tooltip = def.STooltip();
		if (!string.IsNullOrEmpty(tooltip))
		{
			TipSignal tip = new TipSignal(() => tooltip, 275);
			TooltipHandler.TipRegion(rect, tip);
		}
	}

	private void BlockRemove<T>(Rect rect, T def, ref T selectedDef, Action<T> removeAction)
	{
		Rect butRect = new Rect(rect.x + rect.width, rect.y, DefSelectionLineHeight, DefSelectionLineHeight);
		if (Widgets.ButtonImage(butRect, texRemove, Color.grey) && removeAction != null)
		{
			removeAction(def);
			selectedDef = default(T);
		}
		GUI.color = Color.white;
	}

	private void BlockSelectClick<T>(Rect rect, T def, ref T selectedDef)
	{
		if (Widgets.ButtonInvisible(rect))
		{
			selectedDef = ((selectedDef != null) ? default(T) : def);
		}
	}

	private void BLockNumericValue<T>(bool isInt, ref float value, ref float secValue, float min, float max)
	{
		curY += DefSelectionLineHeight;
		if (isInt)
		{
			value = SZWidgets.NumericIntBox(curX, curY, 80f, 30f, (int)value, (int)min, (int)max);
		}
		else
		{
			value = SZWidgets.NumericFloatBox(curX, curY, 70f, 30f, value, min, max);
		}
		if (typeof(T) == typeof(PawnCapacityDef))
		{
			secValue = SZWidgets.NumericFloatBox(curX + 150f, curY, 70f, 30f, secValue, min, max);
		}
		curY += DefSelectionLineHeight;
	}

	internal void ListViewParam1<T>(float width, T def, ref T selectedDef, bool bRemoveOnClick, Action<T> removeAction) where T : Def
	{
		float value = float.MinValue;
		ListViewParam(width, def, ref selectedDef, ref value, ref value, value, value, isInt: false, bRemoveOnClick, removeAction);
	}

	internal void ListViewParam<T>(float width, T def, ref T selectedDef, ref float value, ref float secValue, float min, float max, bool isInt, bool bRemoveOnClick, Action<T> removeAction) where T : Def
	{
		Texture2D tIcon = def.GetTIcon();
		int offset = 0;
		if (tIcon != null)
		{
			Rect rect = new Rect(curX, curY, DefSelectionLineHeight, DefSelectionLineHeight);
			GUI.color = def.GetTColor();
			GUI.DrawTexture(rect, (Texture)def.GetTIcon());
			GUI.color = Color.white;
			offset = (int)DefSelectionLineHeight;
		}
		Rect rect2 = BlockLabel(offset, width, def, value, secValue);
		if (def == selectedDef && value != float.MinValue)
		{
			BLockNumericValue<T>(isInt, ref value, ref secValue, min, max);
		}
		BlockTooltip(rect2, def);
		Text.Anchor = TextAnchor.UpperLeft;
		curY += DefSelectionLineHeight;
		if (bRemoveOnClick)
		{
			BlockRemove(rect2, def, ref selectedDef, removeAction);
		}
		else
		{
			BlockSelectClick(rect2, def, ref selectedDef);
		}
	}

	internal bool ListviewSM(float width, StatModifier sm, bool selected, bool withRemove, Action<string> action)
	{
		if (sm == null || sm.stat == null)
		{
			return false;
		}
		Text.Font = GameFont.Small;
		Text.Anchor = TextAnchor.MiddleLeft;
		Rect rect = new Rect(curX, curY, width - DefSelectionLineHeight, DefSelectionLineHeight);
		Text.WordWrap = false;
		Widgets.Label(rect, sm.stat.label + ": " + sm.value);
		Text.WordWrap = true;
		if (selected)
		{
			curY += DefSelectionLineHeight;
			float min = ((sm.value < sm.stat.minValue) ? (sm.value - 10f) : sm.stat.minValue);
			sm.value = SZWidgets.NumericFloatBox(curX, curY, 70f, 30f, sm.value, min, sm.stat.maxValue);
			curY += DefSelectionLineHeight;
		}
		string tooltip = sm.stat.label + "\n" + sm.stat.category.label.Colorize(Color.yellow) + "\n\n";
		tooltip += sm.stat.description;
		TooltipHandler.TipRegion(tip: new TipSignal(() => tooltip, sm.stat.shortHash), rect: rect);
		Text.Anchor = TextAnchor.UpperLeft;
		curY += DefSelectionLineHeight;
		if (withRemove)
		{
			Rect butRect = new Rect(rect.x + rect.width, rect.y, DefSelectionLineHeight, DefSelectionLineHeight);
			if (Widgets.ButtonImage(butRect, texRemove, Color.grey))
			{
				action?.Invoke(sm.stat.defName);
			}
			GUI.color = Color.white;
		}
		return Widgets.ButtonInvisible(rect);
	}

	private string GetFormattedValue(string format, float value)
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

	internal void AddMultiplierSection(string paramName, string format, ref string selectedName, float baseValue, ref float value, float min = float.MinValue, float max = float.MaxValue, bool small = false)
	{
		Text.Font = (small ? GameFont.Small : GameFont.Medium);
		Rect butRect = new Rect(curX, curY, listingRect.width - 130f, 30f);
		Rect rect = new Rect(curX, curY, listingRect.width, 30f);
		float num = baseValue * value;
		string s = paramName + GetFormattedValue(format, num);
		Widgets.Label(rect, s.Colorize((num > 0f) ? Color.green : ((num == 0f) ? Color.grey : Color.red)));
		if (paramName == selectedName)
		{
			Text.Font = GameFont.Small;
			value = SZWidgets.NumericFloatBox(curX + listingRect.width - 105f, curY + 4f, 70f, 25f, value, min, float.MaxValue);
		}
		curY += 30f;
		Rect rect2 = new Rect(curX, curY, listingRect.width, 20f);
		Widgets.DrawBoxSolid(new Rect(rect2.x, rect2.y, rect2.width, rect2.height / 2f), new Color(0.195f, 0.195f, 0.193f));
		value = Widgets.HorizontalSlider(rect2, value, min, max);
		string text = value.ToString().SubstringFrom(".");
		if (text.Length > 2)
		{
			value = (float)Math.Round(value, 2);
		}
		curY += 20f;
		if (Widgets.ButtonInvisible(butRect))
		{
			selectedName = ((selectedName == paramName) ? null : paramName);
		}
	}

	internal void AddSection(string paramName, string format, ref string selectedName, ref float value, float min = float.MinValue, float max = float.MaxValue, bool small = false, string toolTip = "")
	{
		Text.Font = (small ? GameFont.Small : GameFont.Medium);
		Rect butRect = new Rect(curX, curY, listingRect.width - 130f, 30f);
		Rect rect = new Rect(curX, curY, listingRect.width, 30f);
		paramName = paramName ?? "";
		Widgets.Label(rect, paramName + GetFormattedValue(format, value));
		if (!toolTip.NullOrEmpty())
		{
			TooltipHandler.TipRegion(rect, toolTip);
		}
		if (paramName == selectedName)
		{
			Text.Font = GameFont.Small;
			value = SZWidgets.NumericFloatBox(curX + listingRect.width - 105f, curY + 4f, 70f, 25f, value, min, float.MaxValue);
		}
		curY += 30f;
		Rect rect2 = new Rect(curX, curY, listingRect.width, 20f);
		value = Widgets.HorizontalSlider(rect2, value, min, max);
		string text = value.ToString().SubstringFrom(".");
		if (text.Length > 2)
		{
			value = (float)Math.Round(value, 2);
		}
		curY += 20f;
		if (Widgets.ButtonInvisible(butRect))
		{
			selectedName = ((selectedName == paramName) ? null : paramName);
		}
	}

	internal void AddIntSection(string paramName, string format, ref string selectedName, ref int value, int min = int.MinValue, int max = int.MaxValue, bool small = false, string toolTip = "", bool tiny = false)
	{
		Text.Font = ((!tiny) ? (small ? GameFont.Small : GameFont.Medium) : GameFont.Tiny);
		Rect rect = new Rect(curX, curY, listingRect.width - 130f, 30f);
		Rect rect2 = new Rect(curX, curY, listingRect.width, 30f);
		Widgets.Label(rect2, paramName + GetFormattedValue(format, value));
		if (paramName == selectedName)
		{
			Text.Font = GameFont.Small;
			value = SZWidgets.NumericIntBox(curX + listingRect.width - 105f, curY + 4f, 70f, 25f, value, min, int.MaxValue);
		}
		curY += 30f;
		Rect rect3 = new Rect(curX, curY, listingRect.width, 20f);
		value = (int)Widgets.HorizontalSlider(rect3, value, min, max);
		curY += 20f;
		if (Widgets.ButtonInvisible(rect))
		{
			selectedName = ((selectedName == paramName) ? null : paramName);
		}
		if (!toolTip.NullOrEmpty())
		{
			TooltipHandler.TipRegion(rect, toolTip);
		}
	}

	internal bool RadioButton(string label, bool active, float tabIn = 0f, string tooltip = null)
	{
		float lineHeight = Text.LineHeight;
		Rect rect = GetRect(lineHeight);
		rect.xMin += tabIn;
		if (!tooltip.NullOrEmpty())
		{
			if (Mouse.IsOver(rect))
			{
				Widgets.DrawHighlight(rect);
			}
			TooltipHandler.TipRegion(rect, tooltip);
		}
		bool result = Widgets.RadioButtonLabeled(rect, label, active);
		Gap(verticalSpacing);
		return result;
	}

	internal bool SelectableFast(string text, bool selected, string tooltip)
	{
		Rect rect = new Rect(curX, curY, listingRect.width, DefSelectionLineHeight);
		if (selected)
		{
			GUI.DrawTexture(rect, (Texture)TexUI.TextBGBlack);
			GUI.color = Color.green;
			Widgets.DrawHighlight(rect);
		}
		else
		{
			GUI.color = Color.white;
		}
		Widgets.Label(rect, text);
		TooltipHandler.TipRegion(rect, tooltip);
		curY += DefSelectionLineHeight;
		return Widgets.ButtonInvisible(rect);
	}

	internal bool SelectableAbility(string name, bool selected, string tooltip, AbilityDef def, Color selColor)
	{
		GUI.color = Color.white;
		Text.Anchor = TextAnchor.UpperLeft;
		float width = listingRect.width;
		Rect rect = new Rect(curX, curY, 64f, 64f);
		Rect rect2 = new Rect(rect.x, rect.y, width, 64f);
		GUI.DrawTexture(rect, (Texture)def.uiIcon);
		TooltipHandler.TipRegion(rect2, tooltip);
		if (selected)
		{
			GUI.DrawTexture(rect2, (Texture)TexUI.TextBGBlack);
			GUI.color = selColor;
			Widgets.DrawHighlight(rect2);
		}
		GUI.color = Color.white;
		Widgets.Label(new Rect(rect.x + 68f, rect.y + 23f, width - 64f, 28f), name);
		return Widgets.ButtonInvisible(rect2, doMouseoverSound: false);
	}

	internal bool SelectableGene(string name, bool selected, string tooltip, GeneDef def, Color selColor)
	{
		GUI.color = Color.white;
		Text.Anchor = TextAnchor.UpperLeft;
		float width = listingRect.width;
		Rect rect = new Rect(curX, curY, 64f, 64f);
		Rect rect2 = new Rect(rect.x, rect.y, width, 64f);
		GUI.DrawTexture(rect, (Texture)def.Icon, (ScaleMode)0, true, 0f, def.IconColor, 0f, 0f);
		TooltipHandler.TipRegion(rect2, tooltip);
		if (selected)
		{
			GUI.DrawTexture(rect2, (Texture)TexUI.TextBGBlack);
			GUI.color = selColor;
			Widgets.DrawHighlight(rect2);
		}
		GUI.color = Color.white;
		Widgets.Label(new Rect(rect.x + 68f, rect.y + 23f, width - 72f, 38f), name);
		return Widgets.ButtonInvisible(rect2, doMouseoverSound: false);
	}

	internal bool SelectableThing(string name, bool selected, string tooltip, ThingDef def, Color selColor)
	{
		GUI.color = Color.white;
		Text.Anchor = TextAnchor.UpperLeft;
		float width = listingRect.width;
		Rect rect = new Rect(curX, curY, 64f, 64f);
		Rect rect2 = new Rect(rect.x, rect.y, width, 64f);
		Texture2D tIcon = def.GetTIcon();
		if (tIcon != null)
		{
			GUI.DrawTexture(rect, (Texture)tIcon, (ScaleMode)0, true, 0f, def.uiIconColor, 0f, 0f);
		}
		TooltipHandler.TipRegion(rect2, tooltip);
		if (selected)
		{
			GUI.DrawTexture(rect2, (Texture)TexUI.TextBGBlack);
			GUI.color = selColor;
			Widgets.DrawHighlight(rect2);
		}
		GUI.color = Color.white;
		Widgets.Label(new Rect(rect.x + 68f, rect.y + 23f, width - 72f, 38f), name);
		return Widgets.ButtonInvisible(rect2, doMouseoverSound: false);
	}

	internal bool SelectableText<T>(string name, bool isThing, bool isAbility, bool isGene, bool isHair, bool isBeard, bool selected, string tooltip, bool withRemove, bool isWhite, T def, Color selColor, bool hasIcon = false, bool selectOnMouseOver = false)
	{
		if (name == null)
		{
			return false;
		}
		float width = listingRect.width;
		Rect rect = new Rect(curX, curY, width, DefSelectionLineHeight);
		try
		{
			if (isAbility)
			{
				return SelectableAbility(name, selected, tooltip, def as AbilityDef, selColor);
			}
			if (isGene)
			{
				return SelectableGene(name, selected, tooltip, def as GeneDef, selColor);
			}
			if (isThing)
			{
				return SelectableThing(name, selected, tooltip, def as ThingDef, selColor);
			}
			if (selected)
			{
				GUI.DrawTexture(rect, (Texture)TexUI.TextBGBlack);
				GUI.color = selColor;
				Widgets.DrawHighlight(rect);
			}
			if (hasIcon)
			{
				Rect outerRect = new Rect(curX, curY, DefSelectionLineHeight, DefSelectionLineHeight);
				Texture2D tIcon = def.GetTIcon();
				if (tIcon != null)
				{
					GUI.color = def.GetTColor();
					Widgets.DrawTextureFitted(outerRect, tIcon, 1f);
				}
			}
			Text.WordWrap = false;
			GUI.color = ((!isWhite) ? Color.gray : (selected ? Color.green : Color.white));
			if (hasIcon || isThing)
			{
				Widgets.Label(new Rect(rect.x + DefSelectionLineHeight, rect.y, rect.width, rect.height), name);
			}
			else
			{
				Widgets.Label(rect, name);
			}
			GUI.color = Color.white;
			if (!string.IsNullOrEmpty(tooltip))
			{
				TooltipHandler.TipRegion(rect, tooltip);
			}
			Text.WordWrap = true;
			bool flag = false;
			if (isHair)
			{
				Rect rect2 = new Rect(curX, curY + DefSelectionLineHeight, 192f, 64f);
				Widgets.DrawBoxSolid(rect2, ColorTool.colAsche);
				string texPath = (def as HairDef).texPath;
				if (!texPath.NullOrEmpty())
				{
					Graphic g = GraphicDatabase.Get<Graphic_Multi>(texPath);
					Rect rect3 = new Rect(curX, curY + DefSelectionLineHeight, 64f, 64f);
					Texture2D textureFromMulti = g.GetTextureFromMulti();
					GUI.color = Color.white;
					if (textureFromMulti != null)
					{
						GUI.DrawTexture(rect3, (Texture)textureFromMulti);
					}
					Rect rect4 = new Rect(curX + 64f, curY + DefSelectionLineHeight, 64f, 64f);
					Texture2D textureFromMulti2 = g.GetTextureFromMulti("_east");
					if (textureFromMulti2 == null)
					{
						textureFromMulti2 = g.GetTextureFromMulti("_west");
					}
					GUI.color = Color.white;
					if (textureFromMulti2 != null)
					{
						GUI.DrawTexture(rect4, (Texture)textureFromMulti2);
					}
					Rect rect5 = new Rect(curX + 128f, curY + DefSelectionLineHeight, 64f, 64f);
					Texture2D textureFromMulti3 = g.GetTextureFromMulti("_north");
					GUI.color = Color.white;
					if (textureFromMulti3 != null)
					{
						GUI.DrawTexture(rect5, (Texture)textureFromMulti3);
					}
				}
				if (selectOnMouseOver && Mouse.IsOver(rect2))
				{
					flag = true;
				}
			}
			if (isBeard)
			{
				Graphic g2 = GraphicDatabase.Get<Graphic_Multi>((def as BeardDef).texPath);
				Rect rect6 = new Rect(curX, curY + DefSelectionLineHeight, 192f, 64f);
				Widgets.DrawBoxSolid(rect6, ColorTool.colAsche);
				Rect rect7 = new Rect(curX, curY + DefSelectionLineHeight, 64f, 64f);
				Texture2D textureFromMulti4 = g2.GetTextureFromMulti();
				GUI.color = Color.white;
				GUI.DrawTexture(rect7, (Texture)textureFromMulti4);
				Rect rect8 = new Rect(curX + 64f, curY + DefSelectionLineHeight, 64f, 64f);
				Texture2D textureFromMulti5 = g2.GetTextureFromMulti("_east");
				if (textureFromMulti5 == null)
				{
					textureFromMulti5 = g2.GetTextureFromMulti("_west");
				}
				GUI.color = Color.white;
				GUI.DrawTexture(rect8, (Texture)textureFromMulti5);
				Rect rect9 = new Rect(curX + 128f, curY + DefSelectionLineHeight, 64f, 64f);
				Texture2D textureFromMulti6 = g2.GetTextureFromMulti("_north");
				GUI.DrawTexture(rect9, (Texture)textureFromMulti6);
				if (selectOnMouseOver && Mouse.IsOver(rect6))
				{
					flag = true;
				}
			}
			if (withRemove)
			{
				Rect butRect = new Rect(rect.x + rect.width - DefSelectionLineHeight - 12f, rect.y, DefSelectionLineHeight, DefSelectionLineHeight);
				return Widgets.ButtonImage(butRect, texRemove);
			}
			if (selectOnMouseOver && flag)
			{
				return true;
			}
			return Widgets.ButtonInvisible(rect, doMouseoverSound: false);
		}
		catch
		{
			return false;
		}
	}

	internal int SelectableHorizontal(string name, bool selected, string tooltip = "", RenderTexture image = null, ThingDef thingDef = null, HairDef hairDef = null, Vector2 imageSize = default(Vector2), bool withRemove = false, float selectHeight = 22f, Color backColor = default(Color))
	{
		if (name == null)
		{
			return 0;
		}
		Text.Font = GameFont.Small;
		float num = listingRect.width - DefSelectionLineHeight;
		Text.Anchor = TextAnchor.MiddleLeft;
		Rect rect = new Rect(curX, curY, num + DefSelectionLineHeight, selectHeight);
		if (selected)
		{
			GUI.DrawTexture(rect, (Texture)TexUI.TextBGBlack);
			GUI.color = Color.green;
			Widgets.DrawHighlight(rect);
		}
		Rect rect2 = ((thingDef != null) ? new Rect(curX + 25f, curY, num, DefSelectionLineHeight) : ((selectHeight != 22f) ? new Rect(curX, curY, num + DefSelectionLineHeight, DefSelectionLineHeight) : new Rect(curX, curY, num, DefSelectionLineHeight)));
		Text.WordWrap = false;
		if (backColor != default(Color))
		{
			GUI.color = backColor;
		}
		Widgets.Label(rect2, name);
		GUI.color = Color.white;
		if (!string.IsNullOrEmpty(tooltip))
		{
			TooltipHandler.TipRegion(rect2, tooltip);
		}
		Text.WordWrap = true;
		if (image != null)
		{
			Rect rect3 = ((!(imageSize == default(Vector2))) ? new Rect(curX, curY + DefSelectionLineHeight, imageSize.x, imageSize.y) : new Rect(curX, curY + DefSelectionLineHeight, 64f, 90f));
			GUI.color = Color.white;
			GUI.DrawTexture(rect3, (Texture)image);
		}
		Text.Anchor = TextAnchor.UpperLeft;
		if (image != null)
		{
			if (imageSize == default(Vector2))
			{
				curX += 100f;
			}
			else
			{
				curX += imageSize.x;
			}
		}
		if (withRemove)
		{
			Rect butRect = new Rect(rect.x + rect.width - DefSelectionLineHeight, rect.y, DefSelectionLineHeight, DefSelectionLineHeight);
			if (Widgets.ButtonImage(butRect, texRemove))
			{
				return 2;
			}
			if (Widgets.ButtonInvisible(rect, doMouseoverSound: false))
			{
				return 1;
			}
			return 0;
		}
		if (Widgets.ButtonInvisible(rect, doMouseoverSound: false))
		{
			return 1;
		}
		return 0;
	}

	internal int Selectable(string name, bool selected, string tooltip = "", RenderTexture image = null, ThingDef thingDef = null, HairDef hairDef = null, Vector2 imageSize = default(Vector2), bool withRemove = false, float selectHeight = 22f, Color backColor = default(Color), Color selectedColor = default(Color), bool autoincrement = true)
	{
		if (name == null)
		{
			return 0;
		}
		Text.Font = GameFont.Small;
		float num = listingRect.width - DefSelectionLineHeight;
		Text.Anchor = TextAnchor.MiddleLeft;
		Rect rect = new Rect(curX, curY, num + DefSelectionLineHeight, selectHeight);
		if (selected)
		{
			GUI.DrawTexture(rect, (Texture)TexUI.TextBGBlack);
			GUI.color = selectedColor;
			Widgets.DrawHighlight(rect);
		}
		GUI.color = Color.white;
		if (thingDef != null)
		{
			Rect rect2 = new Rect(curX, curY, DefSelectionLineHeight, DefSelectionLineHeight);
			Widgets.ThingIcon(rect2, thingDef);
		}
		Rect rect3 = ((thingDef != null) ? new Rect(curX + 25f, curY, num, DefSelectionLineHeight) : ((selectHeight != 22f) ? new Rect(curX, curY, num + DefSelectionLineHeight, (name.Length > 10) ? (DefSelectionLineHeight + 12f) : DefSelectionLineHeight) : new Rect(curX, curY, num, DefSelectionLineHeight)));
		if (backColor != default(Color))
		{
			GUI.color = backColor;
		}
		Widgets.Label(rect3, name);
		GUI.color = Color.white;
		if (!string.IsNullOrEmpty(tooltip))
		{
			TooltipHandler.TipRegion(rect, tooltip);
		}
		if (image != null)
		{
			float num2 = (listingRect.width - imageSize.x) / 2f;
			Rect rect4 = ((!(imageSize == default(Vector2))) ? new Rect(curX + num2, curY + 11f, imageSize.x, imageSize.y) : new Rect(curX, curY + DefSelectionLineHeight, 64f, 90f));
			GUI.color = Color.white;
			GUI.DrawTexture(rect4, (Texture)image);
		}
		if (hairDef != null)
		{
			Rect rect5 = new Rect(curX, curY + DefSelectionLineHeight, 64f, 64f);
			Graphic g = GraphicDatabase.Get<Graphic_Multi>(hairDef.texPath);
			Texture2D textureFromMulti = g.GetTextureFromMulti();
			GUI.color = Color.white;
			GUI.DrawTexture(rect5, (Texture)textureFromMulti);
			Rect rect6 = new Rect(curX + 64f, curY + DefSelectionLineHeight, 64f, 64f);
			Texture2D textureFromMulti2 = g.GetTextureFromMulti("_east");
			if (textureFromMulti2 == null)
			{
				textureFromMulti2 = g.GetTextureFromMulti("_west");
			}
			GUI.color = Color.white;
			GUI.DrawTexture(rect6, (Texture)textureFromMulti2);
			Rect rect7 = new Rect(curX + 128f, curY + DefSelectionLineHeight, 64f, 64f);
			Texture2D textureFromMulti3 = g.GetTextureFromMulti("_north");
			GUI.color = Color.white;
			GUI.DrawTexture(rect7, (Texture)textureFromMulti3);
		}
		Text.Anchor = TextAnchor.UpperLeft;
		if (autoincrement)
		{
			curY += DefSelectionLineHeight;
			if (image != null)
			{
				if (imageSize == default(Vector2))
				{
					curY += 90f;
				}
				else
				{
					curY += imageSize.y - DefSelectionLineHeight;
				}
			}
			else if (hairDef != null)
			{
				curY += 70f;
			}
		}
		if (withRemove)
		{
			Rect butRect = new Rect(rect.x + rect.width - DefSelectionLineHeight, rect.y + (float)((name.Length > 20) ? 6 : 0), DefSelectionLineHeight, DefSelectionLineHeight);
			if (Widgets.ButtonImage(butRect, texRemove))
			{
				return 2;
			}
			if (Widgets.ButtonInvisible(rect, doMouseoverSound: false))
			{
				return 1;
			}
			return 0;
		}
		if (Widgets.ButtonInvisible(rect, doMouseoverSound: false))
		{
			return 1;
		}
		return 0;
	}

	internal bool TableLine(Texture2D col0Icon, string col1Val, string col2Val, string col3Val, Texture2D col4Tex, string tooltip)
	{
		Text.Font = GameFont.Small;
		float num = listingRect.width - DefSelectionLineHeight;
		float num2 = (int)(num / 3f);
		float defSelectionLineHeight = DefSelectionLineHeight;
		Rect rect = new Rect(curX, curY, defSelectionLineHeight, defSelectionLineHeight);
		Rect rect2 = new Rect(curX + defSelectionLineHeight, curY, num2, defSelectionLineHeight);
		Rect rect3 = new Rect(curX + num2 + defSelectionLineHeight, curY, num2, defSelectionLineHeight);
		Rect rect4 = new Rect(curX + num2 * 2f + defSelectionLineHeight, curY, num2, defSelectionLineHeight);
		Rect butRect = new Rect(curX + num2 * 3f + defSelectionLineHeight, curY, defSelectionLineHeight, defSelectionLineHeight);
		GUI.DrawTexture(rect, (Texture)col0Icon);
		Widgets.Label(rect2, col1Val);
		Widgets.Label(rect3, col2Val);
		Widgets.Label(rect4, col3Val);
		bool result = Widgets.ButtonImage(butRect, col4Tex);
		if (!string.IsNullOrEmpty(tooltip))
		{
			TipSignal tip = new TipSignal(() => tooltip, 2347778);
			TooltipHandler.TipRegion(rect3, tip);
		}
		curY += DefSelectionLineHeight;
		return result;
	}

	internal bool SelectableThought(string name, Texture2D icon, Color iconColor, float valopin = float.MinValue, float valmood = float.MinValue, string tooltip = "", string pName = "", bool withRemove = true)
	{
		Text.Font = GameFont.Small;
		float num = listingRect.width - DefSelectionLineHeight;
		Text.Anchor = TextAnchor.MiddleLeft;
		Rect rect = new Rect(curX, curY, num + DefSelectionLineHeight, DefSelectionLineHeight);
		GUI.color = iconColor;
		if (icon != null)
		{
			GUI.DrawTexture(new Rect(rect.x, rect.y, 24f, 24f), (Texture)icon);
		}
		Text.WordWrap = false;
		Rect rect2 = new Rect(curX + 30f, curY, num, DefSelectionLineHeight);
		Widgets.DrawBoxSolid(rect2, alternate ? new Color(0.15f, 0.15f, 0.15f) : new Color(0.1f, 0.1f, 0.1f));
		alternate = !alternate;
		Widgets.Label(rect2, name);
		if (tooltip != null)
		{
			TooltipHandler.TipRegion(tip: new TipSignal(() => tooltip, 275), rect: rect2);
		}
		if (!string.IsNullOrEmpty(pName))
		{
			Rect rect3 = new Rect(rect2.x + rect2.width - 190f, rect2.y - 1f, 120f, 24f);
			Widgets.Label(rect3, pName);
		}
		bool flag = valopin != float.MinValue;
		float num2 = (flag ? valopin : valmood);
		int num3 = ((num2 < 0f) ? 4 : 0);
		Rect rect4 = new Rect(rect2.x + rect2.width - 80f - (float)num3, rect2.y, 48f, 24f);
		if (num2 == 0f)
		{
			GUI.color = (flag ? ColorTool.colBeige : NoEffectColor);
		}
		else if (num2 > 0f)
		{
			GUI.color = (flag ? ColorTool.colLightBlue : MoodColor);
		}
		else
		{
			GUI.color = (flag ? ColorTool.colPink : MoodColorNegative);
		}
		Widgets.Label(rect4, num2.ToString("##0"));
		bool result = false;
		if (withRemove)
		{
			Rect butRect = new Rect(rect2.x + rect2.width - 42f, rect2.y, 24f, 24f);
			result = Widgets.ButtonImage(butRect, ContentFinder<Texture2D>.Get("UI/Buttons/Delete"));
		}
		Text.WordWrap = true;
		Text.Anchor = TextAnchor.UpperLeft;
		curY += DefSelectionLineHeight;
		return result;
	}

	internal float Slider(float val, float min, float max, Color color)
	{
		GUI.color = color;
		float result = Widgets.HorizontalSlider(GetRect(22f), val, min, max);
		Gap(verticalSpacing);
		return result;
	}

	internal float Slider(float val, float min, float max)
	{
		float result = Widgets.HorizontalSlider(GetRect(22f), val, min, max);
		Gap(verticalSpacing);
		return result;
	}

	internal float Slider(float val, float min, float max, float width)
	{
		Rect rect = GetRect(22f);
		rect.width = width;
		return Widgets.HorizontalSlider(rect, val, min, max);
	}

	internal float SliderWithNumeric(float val, float min, float max, int decimals)
	{
		float value = (float)Math.Round(Widgets.HorizontalSlider(GetRect(22f), val, min, max), decimals);
		Rect rect = GetRect(22f);
		rect.width = 70f;
		value = SZWidgets.NumericFloatBox(rect, value, float.MinValue, float.MaxValue);
		Gap(4f);
		return value;
	}

	internal int SliderWithNumeric(int val, int min, int max)
	{
		int value = (int)Widgets.HorizontalSlider(GetRect(22f), val, min, max);
		Rect rect = GetRect(22f);
		rect.width = 70f;
		value = SZWidgets.NumericIntBox(rect, value, int.MinValue, int.MaxValue);
		Gap(4f);
		return value;
	}

	internal string TextEntry(string text, int lineCount = 1)
	{
		Rect rect = GetRect(Text.LineHeight * (float)lineCount);
		string result = ((lineCount != 1) ? Widgets.TextArea(rect, text) : Widgets.TextField(rect, text));
		Gap(verticalSpacing);
		return result;
	}

	internal string TextEntryLabeledWithDefaultAndCopy(string label, string text, string defaultVal)
	{
		Rect rect = GetRect(Text.LineHeight * 1f);
		rect.width -= 50f;
		Rect rect2 = rect.LeftHalf().Rounded();
		Rect rect3 = rect.RightHalf().Rounded();
		Widgets.Label(rect2, label);
		string text2 = Widgets.TextField(rect3, text);
		string[] array = text2.SplitNoEmpty(";");
		if (array.Length > 1)
		{
			TooltipHandler.TipRegion(rect2, label + "\n" + array.Length + " saved entities");
		}
		else
		{
			TooltipHandler.TipRegion(rect2, label + "\n" + array.Length + " saved entity");
		}
		Rect rect4 = new Rect(rect.x + rect.width, rect.y, 24f, 24f);
		if (Widgets.ButtonImage(rect4, ContentFinder<Texture2D>.Get("bdefault")))
		{
			text2 = defaultVal;
		}
		TooltipHandler.TipRegion(rect4, CharacterEditor.Label.O_SETTODEFAULT);
		Rect rect5 = new Rect(rect.x + rect.width + 25f, rect.y, 24f, 24f);
		if (Widgets.ButtonImage(rect5, ContentFinder<Texture2D>.Get("UI/Buttons/Copy")))
		{
			Clipboard.CopyToClip(text);
		}
		TooltipHandler.TipRegion(rect5, CharacterEditor.Label.O_COPYTOCLIPBOARD);
		Gap(verticalSpacing);
		return text2;
	}

	internal string TextEntryLabeled(string label, string text, int lineCount = 1)
	{
		Rect rect = GetRect(Text.LineHeight * (float)lineCount);
		string result = Widgets.TextEntryLabeled(rect, label, text);
		Gap(verticalSpacing);
		return result;
	}

	internal void TextFieldNumeric<T>(float xStart, float width, ref T val, ref string buffer, float min = 0f, float max = 1E+09f) where T : struct
	{
		Rect rect = GetRect(Text.LineHeight);
		rect.x = xStart;
		rect.width = width;
		Widgets.TextFieldNumeric(rect, ref val, ref buffer, min, max);
		Gap(verticalSpacing);
	}

	internal void TextFieldNumericLabeled<T>(string label, float xStart, float width, ref T val, ref string buffer, float min = 0f, float max = 1E+09f) where T : struct
	{
		Rect rect = GetRect(Text.LineHeight);
		rect.x = xStart;
		rect.width = width;
		Widgets.TextFieldNumericLabeled(rect, label, ref val, ref buffer, min, max);
		Gap(verticalSpacing);
	}

	private Vector2 GetLabelScrollbarPosition(float x, float y)
	{
		if (labelScrollbarPositions == null)
		{
			return Vector2.zero;
		}
		for (int i = 0; i < labelScrollbarPositions.Count; i++)
		{
			Vector2 first = labelScrollbarPositions[i].First;
			if (first.x == x && first.y == y)
			{
				return labelScrollbarPositions[i].Second;
			}
		}
		return Vector2.zero;
	}

	private void SetLabelScrollbarPosition(float x, float y, Vector2 scrollbarPosition)
	{
		if (labelScrollbarPositions == null)
		{
			labelScrollbarPositions = new List<Pair<Vector2, Vector2>>();
			labelScrollbarPositionsSetThisFrame = new List<Vector2>();
		}
		labelScrollbarPositionsSetThisFrame.Add(new Vector2(x, y));
		for (int i = 0; i < labelScrollbarPositions.Count; i++)
		{
			Vector2 first = labelScrollbarPositions[i].First;
			if (first.x == x && first.y == y)
			{
				labelScrollbarPositions[i] = new Pair<Vector2, Vector2>(new Vector2(x, y), scrollbarPosition);
				return;
			}
		}
		labelScrollbarPositions.Add(new Pair<Vector2, Vector2>(new Vector2(x, y), scrollbarPosition));
	}

	internal void NavSelectorColor(int width, string label, string tip, Color? color, Action onClicked)
	{
		Rect rect = GetRect(22f);
		rect.width = width;
		Text.Font = GameFont.Small;
		SZWidgets.LabelBackground(RectSolid(rect, showEdit: false), label, ColorTool.colAsche, 0, tip);
		SZWidgets.ButtonSolid(RectRandom(rect), color ?? Color.clear, onClicked);
	}

	internal Rect GetRect2(int w, int h = 22)
	{
		Rect rect = GetRect(h);
		rect.width = w;
		return rect;
	}

	private static Rect RectNext(Rect rect)
	{
		return new Rect(rect.x + rect.width - 22f, rect.y + 2f, 22f, 22f);
	}

	private static Rect RectOnClick(Rect rect, bool hasTexture, int offset, bool showEdit)
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

	private static Rect RectSolid(Rect rect, bool showEdit)
	{
		return new Rect(rect.x + (float)(showEdit ? 21 : 0), rect.y, rect.width - (float)(showEdit ? 40 : 19), 24f);
	}

	private static Rect RectTexture(Rect rect, bool showEdit)
	{
		return new Rect(rect.x + (float)(showEdit ? 25 : 0), rect.y, 24f, 24f);
	}

	private static Rect RectToggle(Rect rect)
	{
		return new Rect(rect.x + rect.width - 42f, rect.y, 22f, 22f);
	}

	private static Rect RectToggleLeft(Rect rect)
	{
		return new Rect(rect.x + rect.width - 67f, rect.y, 22f, 22f);
	}
}
