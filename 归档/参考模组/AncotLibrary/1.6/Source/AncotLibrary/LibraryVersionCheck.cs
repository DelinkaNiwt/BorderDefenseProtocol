using Verse;

namespace AncotLibrary;

public static class LibraryVersionCheck
{
	public static bool VersionCheck(VersionCheckDef versionCheckDef)
	{
		return versionCheckDef.versionRequire > AncotLibrarySettings.version;
	}

	public static void LowVersionWarning(string modName, VersionCheckDef versionCheckDef)
	{
		Find.WindowStack.Add(new Dialog_LowVersionWarning(modName, versionCheckDef.versionRequire));
	}

	public static void VersionCheckAndMakeWarning(string modName, VersionCheckDef versionCheckDef)
	{
		if (VersionCheck(versionCheckDef))
		{
			LowVersionWarning(modName, versionCheckDef);
		}
	}
}
