using JetBrains.Annotations;
using Verse;

namespace HugsLib.Settings;

internal class SettingsProxyMod : Mod
{
	private readonly string entryLabel;

	public ModSettingsPack SettingsPack { get; }

	[UsedImplicitly]
	public SettingsProxyMod(ModContentPack content)
		: base(content)
	{
	}

	public SettingsProxyMod(string entryLabel, ModSettingsPack settingsPack, ModContentPack contentPack)
		: base(contentPack)
	{
		SettingsPack = settingsPack;
		this.entryLabel = entryLabel;
	}

	public override string SettingsCategory()
	{
		return entryLabel;
	}
}
