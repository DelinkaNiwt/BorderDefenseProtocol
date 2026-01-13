using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using HarmonyLib;
using Verse;

namespace HarmonyMod;

[StaticConstructorOnStartup]
public class Main : Mod
{
	public static Settings settings;

	public static Version loadedHarmonyVersion;

	public static string loadingError;

	public static string modVersion;

	static Main()
	{
		loadedHarmonyVersion = null;
		modVersion = ((AssemblyFileVersionAttribute)Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyFileVersionAttribute), inherit: false)).Version;
		string[] HarmonyNames = new string[3] { "0Harmony", "Lib.Harmony", "HarmonyLib" };
		Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault((Assembly a) => HarmonyNames.Contains(a.GetName().Name));
		if (assembly != null)
		{
			loadedHarmonyVersion = assembly.GetName().Version ?? new Version(0, 0, 0, 0);
			string text = SafeLocation(assembly);
			string text2 = SafeLocation(Assembly.GetExecutingAssembly());
			string text3 = text2.Substring(0, text2.LastIndexOfAny(new char[2] { '\\', '/' }) + 1) + "0Harmony.dll";
			Version version = new Version(0, 0, 0, 0);
			try
			{
				if (File.Exists(text3))
				{
					version = AssemblyName.GetAssemblyName(text3).Version ?? new Version(0, 0, 0, 0);
				}
			}
			catch (Exception ex)
			{
				Log.Warning("Could not read version of our 0Harmony.dll from disk: " + ex.Message);
			}
			if (text3 != text && version > loadedHarmonyVersion)
			{
				loadingError = "HARMONY LOADING PROBLEM\n\nAnother Harmony library was loaded before the Harmony mod could.\n\n" + $"Their version: {loadedHarmonyVersion}\nOur version: {version}\n\n" + $"This means that your Harmony version is now downgraded to {loadedHarmonyVersion} regardless of what the Harmony mod provides. " + "You need to update or remove that other loader/mod. The other Harmony was loaded from: " + text;
				if (Regex.IsMatch(text, "data-[0-9A-F]{16}"))
				{
					loadingError += "\n\nThe path looks like Harmony was loaded from memory and not via a file path. This often hints to preloaders like Doorstop or similar.";
				}
				Log.Error(loadingError);
			}
		}
		try
		{
			new Harmony("net.pardeike.rimworld.lib.harmony").PatchAll();
		}
		catch (Exception ex2)
		{
			Log.Error("Lib.Harmony could not be initialized: " + ex2.Message);
		}
	}

	public Main(ModContentPack content)
		: base(content)
	{
		settings = GetSettings<Settings>();
	}

	private static string SafeLocation(Assembly a)
	{
		try
		{
			if (!string.IsNullOrEmpty(a.Location))
			{
				return a.Location;
			}
		}
		catch
		{
		}
		try
		{
			string codeBase = a.GetName().CodeBase;
			if (!string.IsNullOrEmpty(codeBase) && Uri.TryCreate(codeBase, UriKind.Absolute, out var result) && result.IsFile)
			{
				return result.LocalPath;
			}
		}
		catch
		{
		}
		return string.Empty;
	}
}
