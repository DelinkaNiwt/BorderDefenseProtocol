using UnityEngine;
using Verse;

namespace AncotLibrary;

[StaticConstructorOnStartup]
public class Mod_AncotLibrary : Mod
{
	public Mod_AncotLibrary(ModContentPack mcp)
		: base(mcp)
	{
		GetSettings<AncotLibrarySettings>();
	}

	public override void WriteSettings()
	{
		base.WriteSettings();
	}

	public override string SettingsCategory()
	{
		return base.Content.Name + " v" + AncotLibrarySettings.version.ToString("F2");
	}

	public override void DoSettingsWindowContents(Rect inRect)
	{
		AncotLibrarySettings.DoWindowContents(inRect);
	}
}
