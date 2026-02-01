using RimWorld;
using UnityEngine;

namespace RimTalk.UI;

public class OverlayTabLauncher : MainTabWindow
{
	public override void DoWindowContents(Rect inRect)
	{
	}

	public override void PostOpen()
	{
		base.PostOpen();
		RimTalkSettings settings = Settings.Get();
		settings.OverlayEnabled = !settings.OverlayEnabled;
		if (settings.OverlayEnabled)
		{
			settings.OverlayEnabled = true;
		}
		settings.Write();
		Close();
	}
}
