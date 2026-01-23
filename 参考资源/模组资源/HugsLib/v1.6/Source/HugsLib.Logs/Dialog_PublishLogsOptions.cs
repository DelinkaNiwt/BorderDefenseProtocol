using System;
using UnityEngine;
using Verse;

namespace HugsLib.Logs;

public class Dialog_PublishLogsOptions : Window
{
	private const float ToggleVerticalSpacing = 4f;

	private readonly string title;

	private readonly string text;

	private readonly ILogPublisherOptions options;

	public Action OnUpload { get; set; }

	public Action OnCopy { get; set; }

	public Action OnOptionsToggled { get; set; }

	public Action OnPostClose { get; set; }

	public override Vector2 InitialSize => new Vector2(550f, 320f);

	public Dialog_PublishLogsOptions(string title, string text, ILogPublisherOptions options)
	{
		this.title = title;
		this.text = text;
		this.options = options;
		forcePause = true;
		absorbInputAroundWindow = true;
		forceCatchAcceptAndCancelEventEvenIfUnfocused = true;
		doCloseX = true;
		draggable = true;
	}

	public override void PostOpen()
	{
		base.PostOpen();
		UpdateWindowSize();
	}

	public override void PostClose()
	{
		base.PostClose();
		OnPostClose?.Invoke();
	}

	public override void OnAcceptKeyPressed()
	{
		Close();
		OnUpload?.Invoke();
	}

	public override void DoWindowContents(Rect inRect)
	{
		Listing_Standard listing_Standard = new Listing_Standard
		{
			ColumnWidth = inRect.width
		};
		listing_Standard.Begin(inRect);
		Text.Font = GameFont.Medium;
		listing_Standard.Label(title);
		listing_Standard.Gap();
		Text.Font = GameFont.Small;
		listing_Standard.Label(text);
		listing_Standard.Gap(24f);
		options.UseCustomOptions = !AddOptionCheckbox(listing_Standard, "HugsLib_logs_useRecommendedSettings", null, !options.UseCustomOptions, out var changed, 0f);
		if (changed)
		{
			UpdateWindowSize();
			OnOptionsToggled?.Invoke();
		}
		if (options.UseCustomOptions)
		{
			options.UseUrlShortener = AddOptionCheckbox(listing_Standard, "HugsLib_logs_shortUrls", "HugsLib_logs_shortUrls_tip", options.UseUrlShortener, out var changed2, 24f);
			options.IncludePlatformInfo = AddOptionCheckbox(listing_Standard, "HugsLib_logs_platformInfo", "HugsLib_logs_platformInfo_tip", options.IncludePlatformInfo, out changed2, 24f);
			options.AllowUnlimitedLogSize = AddOptionCheckbox(listing_Standard, "HugsLib_logs_unlimitedLogSize", "HugsLib_logs_unlimitedLogSize_tip", options.AllowUnlimitedLogSize, out changed2, 24f);
			options.AuthToken = AddOptionTextField(listing_Standard, "HugsLib_logs_github_token", "HugsLib_logs_github_token_tip", options.AuthToken, 24f);
		}
		listing_Standard.End();
		Vector2 vector = new Vector2((inRect.width - 36f) / 3f, 40f);
		Rect rect = inRect.BottomPartPixels(vector.y);
		Rect rect2 = rect.LeftPartPixels(vector.x);
		if (Widgets.ButtonText(rect2, "Close".Translate()))
		{
			Close();
		}
		Rect rect3 = rect.RightPartPixels(vector.x * 2f + 12f);
		if (options.UseCustomOptions && Widgets.ButtonText(rect3.LeftPartPixels(vector.x), "HugsLib_logs_toClipboardBtn".Translate()))
		{
			Close();
			OnCopy?.Invoke();
		}
		if (Widgets.ButtonText(rect3.RightPartPixels(vector.x), "HugsLib_logs_uploadBtn".Translate()))
		{
			OnAcceptKeyPressed();
		}
	}

	private static bool AddOptionCheckbox(Listing listing, string labelKey, string tooltipKey, bool value, out bool changed, float indent)
	{
		bool checkOn = value;
		Rect rect = listing.GetRect(Text.LineHeight);
		Rect rect2 = rect.RightPartPixels(rect.width - indent).LeftHalf();
		listing.Gap(4f);
		if (tooltipKey != null && Mouse.IsOver(rect2))
		{
			Widgets.DrawHighlight(rect2);
			TooltipHandler.TipRegion(rect2, tooltipKey.Translate());
		}
		Widgets.CheckboxLabeled(rect2, labelKey.Translate(), ref checkOn);
		changed = checkOn != value;
		return checkOn;
	}

	private static string AddOptionTextField(Listing listing, string labelKey, string tooltipKey, string value, float indent)
	{
		listing.Gap(4f);
		Rect rect = listing.GetRect(Text.LineHeight);
		Rect rect2 = rect.RightPartPixels(rect.width - indent);
		Rect rect3 = rect2.LeftPartPixels(222f);
		Rect rect4 = rect2.RightPartPixels(rect2.width - 222f);
		if (tooltipKey != null && Mouse.IsOver(rect2))
		{
			Widgets.DrawHighlight(rect2);
			TooltipHandler.TipRegion(rect2, tooltipKey.Translate());
		}
		Widgets.Label(rect3, labelKey.Translate());
		return Widgets.TextField(rect4, value);
	}

	private void UpdateWindowSize()
	{
		float num = (options.UseCustomOptions ? ((Text.LineHeight + 4f) * 4f) : 0f);
		windowRect = new Rect(windowRect.x, windowRect.y, InitialSize.x, InitialSize.y + num);
	}
}
