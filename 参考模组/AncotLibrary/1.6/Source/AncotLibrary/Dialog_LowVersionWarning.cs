using UnityEngine;
using Verse;

namespace AncotLibrary;

public class Dialog_LowVersionWarning : Window
{
	public TaggedString text;

	public double versionRequire;

	public string title;

	public string buttonAText;

	protected const float ButtonHeight = 35f;

	public override Vector2 InitialSize => new Vector2(800f, 500f);

	public Dialog_LowVersionWarning(TaggedString text, double versionRequire, WindowLayer layer = WindowLayer.Dialog)
	{
		this.text = text;
		this.versionRequire = versionRequire;
		base.layer = layer;
		if (buttonAText.NullOrEmpty())
		{
			buttonAText = "OK".Translate();
		}
		forcePause = false;
		absorbInputAroundWindow = true;
		onlyOneOfTypeAllowed = true;
		closeOnCancel = true;
		closeOnAccept = true;
		doWindowBackground = true;
		closeOnClickedOutside = false;
		draggable = true;
	}

	public override void DoWindowContents(Rect inRect)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Invalid comparison between Unknown and I4
		if ((int)Event.current.type != 8)
		{
			Rect rect = new Rect(inRect.x, inRect.y, inRect.width, 35f);
			using (new TextBlock(GameFont.Medium, TextAnchor.UpperCenter))
			{
				Widgets.Label(rect, "Ancot.LowVersionWarning".Translate());
			}
			Widgets.DrawLineHorizontal(inRect.x, inRect.y + 35f, inRect.width);
			Rect rect2 = new Rect(inRect.x, inRect.y + rect.height + 5f, inRect.width, inRect.height - 35f);
			Widgets.Label(rect2, "Ancot.LowVersionWarningDesc".Translate(text, versionRequire.ToString("F2"), AncotLibrarySettings.version.ToString("F2")));
			if (Widgets.ButtonText(new Rect(inRect.x, inRect.y + inRect.height - 75f, inRect.width, 35f), buttonAText))
			{
				Close();
			}
			if (Widgets.ButtonText(new Rect(inRect.x, inRect.y + inRect.height - 35f, inRect.width, 35f), "Ancot.LowVersionWarningOpenURL".Translate()))
			{
				Application.OpenURL("steam://openurl/https://steamcommunity.com/sharedfiles/filedetails/?id=2988801276");
				Close();
			}
		}
	}
}
