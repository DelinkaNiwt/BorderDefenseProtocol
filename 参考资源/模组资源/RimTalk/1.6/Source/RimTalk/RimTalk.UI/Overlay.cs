using System;
using System.Collections.Generic;
using System.Linq;
using RimTalk.Data;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimTalk.UI;

public class Overlay : MapComponent
{
	private class CachedMessageLine
	{
		public string PawnName;

		public string Dialogue;

		public float NameWidth;

		public float LineHeight;

		public Pawn PawnInstance;
	}

	private bool _isDragging;

	private bool _isResizing;

	private Vector2 _dragStartOffset;

	private bool _showSettingsDropdown;

	private Rect _gearIconScreenRect;

	private Rect _settingsDropdownRect;

	private Rect _dragHandleRect;

	private Rect _localResizeHandleRect;

	private Rect _screenResizeHandleRect;

	private List<CachedMessageLine> _cachedMessagesForLog;

	private bool _isCacheDirty = true;

	private const float OptionsBarHeight = 30f;

	private const float ResizeHandleSize = 24f;

	private const float DropdownWidth = 200f;

	private const float DropdownHeight = 220f;

	private const int MaxMessagesInLog = 10;

	private const float TextPadding = 5f;

	public static event Action OnLogUpdated;

	public static void NotifyLogUpdated()
	{
		Overlay.OnLogUpdated?.Invoke();
	}

	public Overlay(Map map)
		: base(map)
	{
		OnLogUpdated += MarkCacheAsDirty;
	}

	private void MarkCacheAsDirty()
	{
		_isCacheDirty = true;
	}

	public override void MapRemoved()
	{
		base.MapRemoved();
		OnLogUpdated -= MarkCacheAsDirty;
	}

	private void UpdateAndRecalculateCache()
	{
		RimTalkSettings settings = Settings.Get();
		List<ApiLog> allRequests = ApiHistory.GetAll().ToList();
		GameFont originalFont = Text.Font;
		TextAnchor originalAnchor = Text.Anchor;
		GameFont gameFont = GameFont.Small;
		int originalFontSize = Text.fontStyles[(uint)gameFont].fontSize;
		try
		{
			Text.Font = gameFont;
			Text.fontStyles[(uint)gameFont].fontSize = (int)settings.OverlayFontSize;
			Text.Anchor = TextAnchor.UpperLeft;
			float contentWidth = settings.OverlayRectNonDebug.width - 10f;
			List<CachedMessageLine> newCache = new List<CachedMessageLine>();
			IEnumerable<ApiLog> messages = (from r in allRequests.Where((ApiLog r) => r.SpokenTick > 0).Reverse()
				orderby r.SpokenTick descending
				select r).Take(10);
			foreach (ApiLog message in messages)
			{
				string pawnName = message.Name ?? "Unknown";
				string dialogue = message.Response ?? "";
				string formattedName = "[" + pawnName + "]";
				float nameWidth = Text.CalcSize(formattedName).x;
				float availableDialogueWidth = contentWidth - nameWidth - 5f;
				if (availableDialogueWidth < 0f)
				{
					availableDialogueWidth = contentWidth * 0.5f;
				}
				float dialogueWidthForCalc = Mathf.Max(0f, availableDialogueWidth - 3f);
				float dialogueHeight = Text.CalcHeight(dialogue, dialogueWidthForCalc);
				float nameHeight = Text.CalcHeight(formattedName, nameWidth);
				float lineHeight = Mathf.Max(dialogueHeight, nameHeight);
				lineHeight += 2f;
				Pawn foundPawn = global::RimTalk.Data.Cache.GetByName(pawnName)?.Pawn ?? Find.CurrentMap?.mapPawns?.AllPawns?.FirstOrDefault((Pawn p) => p?.Name?.ToStringShort == pawnName) ?? Find.WorldPawns?.AllPawnsAliveOrDead.FirstOrDefault((Pawn p) => p?.Name?.ToStringShort == pawnName);
				newCache.Add(new CachedMessageLine
				{
					PawnName = pawnName,
					Dialogue = dialogue,
					NameWidth = nameWidth,
					LineHeight = lineHeight,
					PawnInstance = foundPawn
				});
			}
			_cachedMessagesForLog = newCache;
		}
		finally
		{
			Text.fontStyles[(uint)gameFont].fontSize = originalFontSize;
			Text.Font = originalFont;
			Text.Anchor = originalAnchor;
		}
		_isCacheDirty = false;
	}

	public override void MapComponentOnGUI()
	{
		if (Current.ProgramState != ProgramState.Playing)
		{
			return;
		}
		RimTalkSettings settings = Settings.Get();
		if (settings.OverlayEnabled)
		{
			ref Rect currentOverlayRect = ref settings.OverlayRectNonDebug;
			if (currentOverlayRect.width <= 0f || currentOverlayRect.height <= 0f)
			{
				currentOverlayRect = new Rect(20f, 20f, 400f, 250f);
			}
			ClampRectToScreen(ref currentOverlayRect);
			float iconSize = 26f;
			_dragHandleRect.Set(currentOverlayRect.x, currentOverlayRect.y, currentOverlayRect.width, 30f);
			_gearIconScreenRect.Set(currentOverlayRect.xMax - iconSize - 5f, currentOverlayRect.y + 2f, iconSize, iconSize);
			float dropdownY = _gearIconScreenRect.yMax;
			if (dropdownY + 220f > (float)Verse.UI.screenHeight)
			{
				dropdownY = _gearIconScreenRect.y - 220f;
			}
			_settingsDropdownRect.Set(_gearIconScreenRect.x - 200f + _gearIconScreenRect.width, dropdownY, 200f, 220f);
			_screenResizeHandleRect.Set(currentOverlayRect.xMax - 24f, currentOverlayRect.yMax - 24f, 24f, 24f);
			HandleInput(ref currentOverlayRect);
			bool isMouseOver = Mouse.IsOver(currentOverlayRect);
			GUI.BeginGroup(currentOverlayRect);
			Rect inRect = new Rect(Vector2.zero, currentOverlayRect.size);
			Widgets.DrawBoxSolid(inRect, new Color(0.1f, 0.1f, 0.1f, settings.OverlayOpacity));
			Rect contentRect = new Rect(inRect.x, inRect.y, inRect.width, inRect.height);
			DrawMessageLog(contentRect);
			if (isMouseOver)
			{
				Rect optionsRect = new Rect(inRect.x, inRect.y, inRect.width, 30f);
				DrawOptionsBar(optionsRect);
				_localResizeHandleRect.Set(inRect.width - 24f, inRect.height - 24f, 24f, 24f);
				GUI.DrawTexture(_localResizeHandleRect, (Texture)TexUI.WinExpandWidget);
				TooltipHandler.TipRegion(_localResizeHandleRect, "Drag to resize");
			}
			GUI.EndGroup();
			if (_showSettingsDropdown)
			{
				DrawSettingsDropdown();
			}
		}
	}

	private void HandleInput(ref Rect windowRect)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Invalid comparison between Unknown and I4
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Invalid comparison between Unknown and I4
		Event currentEvent = Event.current;
		if ((int)currentEvent.type == 0 && currentEvent.button == 0)
		{
			if (_showSettingsDropdown)
			{
				if (_settingsDropdownRect.Contains(currentEvent.mousePosition))
				{
					return;
				}
				if (!_gearIconScreenRect.Contains(currentEvent.mousePosition))
				{
					_showSettingsDropdown = false;
					currentEvent.Use();
					return;
				}
			}
			if (_screenResizeHandleRect.Contains(currentEvent.mousePosition))
			{
				_isResizing = true;
				currentEvent.Use();
			}
			else if (_dragHandleRect.Contains(currentEvent.mousePosition) && !_gearIconScreenRect.Contains(currentEvent.mousePosition))
			{
				_isDragging = true;
				_dragStartOffset = currentEvent.mousePosition - windowRect.position;
				currentEvent.Use();
			}
		}
		else if ((int)currentEvent.type == 1 && currentEvent.button == 0)
		{
			if (_isDragging || _isResizing)
			{
				Settings.Get().Write();
			}
			_isDragging = false;
			_isResizing = false;
		}
		else if ((int)currentEvent.type == 3)
		{
			if (_isResizing)
			{
				float desiredWidth = currentEvent.mousePosition.x - windowRect.x;
				float desiredHeight = currentEvent.mousePosition.y - windowRect.y;
				float maxWidth = (float)Verse.UI.screenWidth - windowRect.x;
				float maxHeight = (float)Verse.UI.screenHeight - windowRect.y;
				windowRect.width = Mathf.Clamp(desiredWidth, 350f, maxWidth);
				windowRect.height = Mathf.Clamp(desiredHeight, 50f, maxHeight);
				_isCacheDirty = true;
				currentEvent.Use();
			}
			else if (_isDragging)
			{
				windowRect.position = currentEvent.mousePosition - _dragStartOffset;
				currentEvent.Use();
			}
			ClampRectToScreen(ref windowRect);
		}
	}

	private void ClampRectToScreen(ref Rect rect)
	{
		rect.x = Mathf.Clamp(rect.x, 0f, (float)Verse.UI.screenWidth - rect.width);
		rect.y = Mathf.Clamp(rect.y, 0f, (float)Verse.UI.screenHeight - rect.height);
	}

	private void DrawOptionsBar(Rect rect)
	{
		float iconSize = rect.height - 4f;
		Rect localIconRect = new Rect(rect.width - iconSize - 2f, 2f, iconSize, iconSize);
		RimTalkSettings settings = Settings.Get();
		float effectiveOpacity = Mathf.Max(settings.OverlayOpacity, 0.3f);
		Texture2D iconTexture = ContentFinder<Texture2D>.Get("UI/Icons/Options/OptionsGeneral");
		Color iconColor = Color.white;
		iconColor.a = effectiveOpacity;
		Color mouseoverColor = GenUI.MouseoverColor;
		mouseoverColor.a = effectiveOpacity;
		if (Widgets.ButtonImage(localIconRect, iconTexture, iconColor, mouseoverColor))
		{
			_showSettingsDropdown = !_showSettingsDropdown;
		}
		TooltipHandler.TipRegion(localIconRect, "RimTalk.Overlay.Option".Translate());
	}

	private void DrawSettingsCheckbox(Listing_Standard listing, string label, bool initialValue, Action<bool> onValueChanged)
	{
		bool currentValue = initialValue;
		listing.CheckboxLabeled(label, ref currentValue);
		if (currentValue != initialValue)
		{
			onValueChanged(currentValue);
		}
	}

	private void DrawSettingsDropdown()
	{
		RimTalkSettings settings = Settings.Get();
		Widgets.DrawBoxSolid(_settingsDropdownRect, new Color(0.15f, 0.15f, 0.15f, 0.95f));
		Listing_Standard listing = new Listing_Standard();
		listing.Begin(_settingsDropdownRect.ContractedBy(10f));
		DrawSettingsCheckbox(listing, "RimTalk.DebugWindow.EnableRimTalk".Translate(), settings.IsEnabled, delegate(bool value)
		{
			settings.IsEnabled = value;
			settings.Write();
		});
		listing.Gap(6f);
		bool overlayDrawAboveUI = settings.OverlayDrawAboveUI;
		listing.CheckboxLabeled("RimTalk.Overlay.DrawAboveUI".Translate(), ref overlayDrawAboveUI);
		if (overlayDrawAboveUI != settings.OverlayDrawAboveUI)
		{
			settings.OverlayDrawAboveUI = overlayDrawAboveUI;
			settings.Write();
		}
		listing.Gap(6f);
		listing.Label("RimTalk.Overlay.Opacity".Translate() + ": " + settings.OverlayOpacity.ToString("P0"));
		settings.OverlayOpacity = listing.Slider(settings.OverlayOpacity, 0f, 1f);
		listing.Label("RimTalk.Overlay.FontSize".Translate() + ": " + settings.OverlayFontSize.ToString("F0"));
		float newFontSize = listing.Slider(Mathf.Round(settings.OverlayFontSize), 10f, 24f);
		if (Mathf.Round(newFontSize) != Mathf.Round(settings.OverlayFontSize))
		{
			_isCacheDirty = true;
			settings.OverlayFontSize = newFontSize;
		}
		listing.Gap();
		Rect buttonRowRect = listing.GetRect(30f);
		float buttonWidth = (buttonRowRect.width - 8f) / 2f;
		Rect debugRect = new Rect(buttonRowRect.x, buttonRowRect.y, buttonWidth, buttonRowRect.height);
		Rect settingsButtonRect = new Rect(debugRect.xMax + 4f, buttonRowRect.y, buttonWidth, buttonRowRect.height);
		if (Widgets.ButtonText(debugRect, "RimTalk.Overlay.Debug".Translate()))
		{
			if (!Find.WindowStack.IsOpen<DebugWindow>())
			{
				Find.WindowStack.Add(new DebugWindow());
			}
			_showSettingsDropdown = false;
		}
		if (Widgets.ButtonText(settingsButtonRect, "RimTalk.DebugWindow.ModSettings".Translate()))
		{
			Find.WindowStack.Add(new Dialog_ModSettings(LoadedModManager.GetMod<Settings>()));
			_showSettingsDropdown = false;
		}
		listing.End();
	}

	private void DrawMessageLog(Rect inRect)
	{
		if (_isCacheDirty)
		{
			UpdateAndRecalculateCache();
		}
		Rect contentRect = inRect.ContractedBy(5f);
		if (_cachedMessagesForLog == null || !_cachedMessagesForLog.Any())
		{
			return;
		}
		RimTalkSettings settings = Settings.Get();
		GameFont originalFont = Text.Font;
		TextAnchor originalAnchor = Text.Anchor;
		GameFont gameFont = GameFont.Small;
		int originalFontSize = Text.fontStyles[(uint)gameFont].fontSize;
		try
		{
			Text.Font = gameFont;
			Text.fontStyles[(uint)gameFont].fontSize = (int)settings.OverlayFontSize;
			Text.Anchor = TextAnchor.UpperLeft;
			float currentY = contentRect.yMax;
			foreach (CachedMessageLine message in _cachedMessagesForLog)
			{
				currentY -= message.LineHeight;
				if (currentY < contentRect.y)
				{
					break;
				}
				Rect rowRect = new Rect(contentRect.x, currentY, contentRect.width, message.LineHeight);
				Rect nameRect = new Rect(rowRect.x, rowRect.y, message.NameWidth, rowRect.height);
				float totalDialogueSpace = Mathf.Max(0f, rowRect.width - message.NameWidth);
				Rect dialogueRect = new Rect(nameRect.xMax + 5f, rowRect.y, totalDialogueSpace - 5f, rowRect.height);
				UIUtil.DrawClickablePawnName(nameRect, message.PawnName, message.PawnInstance);
				Widgets.Label(dialogueRect, message.Dialogue);
			}
		}
		finally
		{
			Text.fontStyles[(uint)gameFont].fontSize = originalFontSize;
			Text.Font = originalFont;
			Text.Anchor = originalAnchor;
		}
	}
}
