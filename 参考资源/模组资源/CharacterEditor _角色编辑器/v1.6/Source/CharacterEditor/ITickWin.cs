using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace CharacterEditor;

internal class ITickWin : Window
{
	private int timeOut = 3000;

	private bool doClose = false;

	public override Vector2 InitialSize => new Vector2(1f, 1f);

	public override void DoWindowContents(Rect inRect)
	{
		if (doClose)
		{
			Close();
			return;
		}
		if (timeOut > 0)
		{
			timeOut--;
			return;
		}
		doClose = true;
		MapParent parent = Find.CurrentMap.Parent;
		parent.Abandon(wasGravshipLaunch: false);
		GenScene.GoToMainMenu();
		Close();
	}
}
