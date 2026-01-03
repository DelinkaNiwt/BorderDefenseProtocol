using UnityEngine;
using Verse;

namespace Milira;

[StaticConstructorOnStartup]
public class Mod_MiliraRace : Mod
{
	public Mod_MiliraRace(ModContentPack mcp)
		: base(mcp)
	{
		GetSettings<MiliraRaceSettings>();
	}

	public override void WriteSettings()
	{
		base.WriteSettings();
	}

	public override string SettingsCategory()
	{
		return base.Content.Name;
	}

	public override void DoSettingsWindowContents(Rect inRect)
	{
		MiliraRaceSettings.DoWindowContents(inRect);
	}
}
