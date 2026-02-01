using RimWorld;
using UnityEngine;

namespace CharacterEditor;

public class MainTabWindow_CharacterEditor : MainTabWindow
{
	public override Vector2 InitialSize => new Vector2(1f, 1f);

	public MainTabWindow_CharacterEditor()
	{
		closeOnAccept = false;
		closeOnCancel = false;
	}

	public override void DoWindowContents(Rect inRect)
	{
		CEditor.API.StartEditor();
		Close();
	}
}
