using System.Collections.Generic;
using HugsLib.Utils;
using UnityEngine;
using Verse;

namespace HugsLib.Logs;

/// <summary>
/// The front-end for LogPublisher.
/// Shows the status of the upload operation, provides controls and shows the produced URL.
/// </summary>
[StaticConstructorOnStartup]
public class Dialog_PublishLogs : Window
{
	private class StatusLabelEntry
	{
		public readonly string labelKey;

		public readonly bool requiresEllipsis;

		public StatusLabelEntry(string labelKey, bool requiresEllipsis)
		{
			this.labelKey = labelKey;
			this.requiresEllipsis = requiresEllipsis;
		}
	}

	private const float StatusLabelHeight = 60f;

	private const int MaxResultUrlLength = 32;

	private static readonly Texture2D UrlBackgroundTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.25f, 0.25f, 0.17f, 0.85f));

	private readonly Vector2 CopyButtonSize = new Vector2(100f, 40f);

	private readonly Vector2 ControlButtonSize = new Vector2(150f, 40f);

	private readonly Dictionary<LogPublisher.PublisherStatus, StatusLabelEntry> statusMessages = new Dictionary<LogPublisher.PublisherStatus, StatusLabelEntry>
	{
		{
			LogPublisher.PublisherStatus.Ready,
			new StatusLabelEntry("", requiresEllipsis: false)
		},
		{
			LogPublisher.PublisherStatus.Uploading,
			new StatusLabelEntry("HugsLib_logs_uploading", requiresEllipsis: true)
		},
		{
			LogPublisher.PublisherStatus.Shortening,
			new StatusLabelEntry("HugsLib_logs_shortening", requiresEllipsis: true)
		},
		{
			LogPublisher.PublisherStatus.Done,
			new StatusLabelEntry("HugsLib_logs_uploaded", requiresEllipsis: false)
		},
		{
			LogPublisher.PublisherStatus.Error,
			new StatusLabelEntry("HugsLib_logs_uploadError", requiresEllipsis: false)
		}
	};

	private readonly LogPublisher publisher;

	public override Vector2 InitialSize => new Vector2(500f, 250f);

	public Dialog_PublishLogs()
	{
		closeOnCancel = true;
		closeOnAccept = false;
		doCloseButton = false;
		doCloseX = true;
		forcePause = true;
		onlyOneOfTypeAllowed = true;
		focusWhenOpened = true;
		draggable = true;
		publisher = HugsLibController.Instance.LogUploader;
	}

	public override void DoWindowContents(Rect inRect)
	{
		Text.Font = GameFont.Medium;
		Rect rect = new Rect(inRect.x, inRect.y, inRect.width, 40f);
		Widgets.Label(rect, "HugsLib_logs_publisherTitle".Translate());
		Text.Font = GameFont.Small;
		StatusLabelEntry statusLabelEntry = statusMessages[publisher.Status];
		TaggedString taggedString = (statusLabelEntry.requiresEllipsis ? statusLabelEntry.labelKey.Translate(GenText.MarchingEllipsis(Time.realtimeSinceStartup)) : statusLabelEntry.labelKey.Translate());
		if (publisher.Status == LogPublisher.PublisherStatus.Error)
		{
			taggedString = string.Format(taggedString, publisher.ErrorMessage);
		}
		Rect rect2 = new Rect(inRect.x, inRect.y + rect.height, inRect.width, 60f);
		Widgets.Label(rect2, taggedString);
		if (publisher.Status == LogPublisher.PublisherStatus.Done)
		{
			Rect rect3 = new Rect(inRect.x, rect2.y + rect2.height, inRect.width, CopyButtonSize.y);
			GUI.DrawTexture(rect3, (Texture)UrlBackgroundTex);
			Rect rect4 = new Rect(rect3.x, rect3.y, rect3.width - CopyButtonSize.x, rect3.height);
			Text.Font = GameFont.Medium;
			TextAnchor anchor = Text.Anchor;
			Text.Anchor = TextAnchor.MiddleCenter;
			string text = publisher.ResultUrl;
			if (text.Length > 32)
			{
				text = text.Substring(0, 32) + "...";
			}
			Widgets.Label(rect4, text);
			Text.Anchor = anchor;
			Text.Font = GameFont.Small;
			Rect rect5 = new Rect(inRect.width - CopyButtonSize.x, rect3.y, CopyButtonSize.x, CopyButtonSize.y);
			if (Widgets.ButtonText(rect5, "HugsLib_logs_copy".Translate()))
			{
				HugsLibUtility.CopyToClipboard(publisher.ResultUrl);
			}
		}
		Rect rect6 = new Rect(inRect.x, inRect.height - ControlButtonSize.y, ControlButtonSize.x, ControlButtonSize.y);
		if (publisher.Status == LogPublisher.PublisherStatus.Error)
		{
			if (Widgets.ButtonText(rect6, "HugsLib_logs_retryBtn".Translate()))
			{
				publisher.BeginUpload();
			}
		}
		else if (publisher.Status == LogPublisher.PublisherStatus.Done && Widgets.ButtonText(rect6, "HugsLib_logs_browseBtn".Translate()))
		{
			Application.OpenURL(publisher.ResultUrl);
		}
		Rect rect7 = new Rect(inRect.width - ControlButtonSize.x, inRect.height - ControlButtonSize.y, ControlButtonSize.x, ControlButtonSize.y);
		if (publisher.Status == LogPublisher.PublisherStatus.Uploading || publisher.Status == LogPublisher.PublisherStatus.Shortening)
		{
			if (Widgets.ButtonText(rect7, "HugsLib_logs_abortBtn".Translate()))
			{
				publisher.AbortUpload();
			}
		}
		else if (Widgets.ButtonText(rect7, "CloseButton".Translate()))
		{
			Close();
		}
	}
}
