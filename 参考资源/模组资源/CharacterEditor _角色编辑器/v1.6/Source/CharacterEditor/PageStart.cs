using RimWorld;
using UnityEngine;

namespace CharacterEditor;

internal class PageStart : Page
{
	public override void DoWindowContents(Rect inRect)
	{
		PageUtility.InitGameStart();
		Close();
	}
}
