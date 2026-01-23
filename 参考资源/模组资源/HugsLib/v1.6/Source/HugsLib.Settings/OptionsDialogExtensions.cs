using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;

namespace HugsLib.Settings;

internal static class OptionsDialogExtensions
{
	private static FieldInfo cachedModsField;

	private static FieldInfo hasModSettingsField;

	public static void InjectHugsLibModEntries(Dialog_Options dialog)
	{
		(ModSettingsPack, ModContentPack)[] array = HugsLibController.Instance.InitializedMods.Where((ModBase mod) => mod.SettingsPackInternalAccess != null).Select(delegate(ModBase mod)
		{
			ModSettingsPack settingsPackInternalAccess = mod.SettingsPackInternalAccess;
			ModContentPack item = mod.ModContentPack ?? HugsLibController.OwnContentPack;
			return (settingsPack: settingsPackInternalAccess, contentPack: item);
		}).Append((HugsLibController.OwnSettingsPack, HugsLibController.OwnContentPack))
			.ToArray();
		IEnumerable<(ModSettingsPack, ModContentPack)> second = from pack in HugsLibController.SettingsManager.ModSettingsPacks.Except(array.Select(((ModSettingsPack settingsPack, ModContentPack contentPack) packs) => packs.settingsPack))
			select (settingsPack: pack, contentPack: HugsLibController.OwnContentPack);
		IEnumerable<SettingsProxyMod> second2 = (from packs in array.Concat(second)
			where packs.settingsPack.Handles.Any((SettingHandle h) => !h.NeverVisible)
			select packs).Select<(ModSettingsPack, ModContentPack), SettingsProxyMod>(delegate((ModSettingsPack settingsPack, ModContentPack contentPack) packs)
		{
			string entryLabel = (packs.settingsPack.EntryName.NullOrEmpty() ? "HugsLib_setting_unnamed_mod".Translate().ToString() : packs.settingsPack.EntryName);
			return new SettingsProxyMod(entryLabel, packs.settingsPack, packs.contentPack);
		});
		IEnumerable<Mod> first = (IEnumerable<Mod>)cachedModsField.GetValue(dialog);
		Mod[] value = (from m in first.Concat(second2)
			orderby m.SettingsCategory()
			select m).ToArray();
		cachedModsField.SetValue(dialog, value);
		hasModSettingsField.SetValue(dialog, true);
	}

	public static Window GetModSettingsWindow(Mod forMod)
	{
		return (forMod is SettingsProxyMod settingsProxyMod) ? ((Window)new Dialog_ModSettings(settingsProxyMod.SettingsPack)) : ((Window)new RimWorld.Dialog_ModSettings(forMod));
	}

	public static void PrepareReflection()
	{
		cachedModsField = typeof(Dialog_Options).GetField("cachedModsWithSettings", BindingFlags.Instance | BindingFlags.NonPublic);
		if (cachedModsField == null || cachedModsField.FieldType != typeof(IEnumerable<Mod>))
		{
			HugsLibController.Logger.Error("Failed to reflect Dialog_Options.cachedModsWithSettings");
		}
		hasModSettingsField = typeof(Dialog_Options).GetField("hasModSettings", BindingFlags.Instance | BindingFlags.NonPublic);
		if (hasModSettingsField == null || hasModSettingsField.FieldType != typeof(bool))
		{
			HugsLibController.Logger.Error("Failed to reflect Dialog_Options.hasModSettings");
		}
	}
}
