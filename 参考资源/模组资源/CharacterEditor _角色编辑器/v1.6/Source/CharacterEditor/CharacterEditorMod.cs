using UnityEngine;
using Verse;

namespace CharacterEditor;

[StaticConstructorOnStartup]
public class CharacterEditorMod : Mod
{
	internal const string MODID = "rimworld.mod.charactereditor";

	public CharacterEditorMod(ModContentPack content)
		: base(content)
	{
		CEditor.Initialize(this);
	}

	public override void DoSettingsWindowContents(Rect inRect)
	{
		CEditor.API.ConfigEditor();
		WindowTool.Close(WindowTool.GetWindowOfEndsWithType("ModSettings"));
	}

	public override string SettingsCategory()
	{
		return "CharacterEditor";
	}
}
