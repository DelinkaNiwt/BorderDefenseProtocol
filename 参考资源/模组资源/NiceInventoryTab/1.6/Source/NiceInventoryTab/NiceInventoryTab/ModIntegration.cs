using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace NiceInventoryTab;

public class ModIntegration : GameComponent
{
	public static readonly string ModLogPrefix = "[Andromeda|NiceInventoryTab] ";

	public static bool VEFActive = false;

	public static bool NUPActive = false;

	public static bool CLActive = false;

	public static bool QCActive = false;

	public ModIntegration(Game game)
	{
	}

	public override void StartedNewGame()
	{
		DoPostPatches();
	}

	public override void LoadedGame()
	{
		DoPostPatches();
	}

	public static void DoPostPatches()
	{
		BillFinished_Patch.Clear();
		if (VEFActive)
		{
			VanillaExpandedFrameworkIntegration.PostPatch();
		}
	}

	public static void FailMessage(string msg)
	{
		Log.Warning(ModLogPrefix + msg);
	}

	public static void DebugLogModAssemblies()
	{
		foreach (ModContentPack runningMod in LoadedModManager.RunningMods)
		{
			IEnumerable<string> values = runningMod.assemblies.loadedAssemblies.Select((Assembly x) => x.GetName().Name);
			string text = string.Join(", ", values);
			Log.Message("mod: '" + runningMod.Name + "' PackageId: '" + runningMod.PackageId + "' assembly: " + text);
		}
	}

	public static Assembly TryGetExternalAssembly(string modPackageId, string modAssembly, string messageIfFailed = null)
	{
		ModContentPack modContentPack = LoadedModManager.RunningMods.FirstOrDefault((ModContentPack mod) => mod.PackageId == modPackageId);
		if (modContentPack == null)
		{
			return null;
		}
		Assembly assembly = modContentPack.assemblies.loadedAssemblies.FirstOrDefault((Assembly ass) => ass.GetName().Name == modAssembly);
		if (assembly == null)
		{
			if (!messageIfFailed.NullOrEmpty())
			{
				Log.Warning(ModLogPrefix + messageIfFailed);
			}
			return null;
		}
		return assembly;
	}

	public static void DoPatches(Harmony harmonyInstance)
	{
		if (TryGetExternalAssembly("oskarpotocki.vanillafactionsexpanded.core", "VEF") != null)
		{
			VEFActive = true;
		}
		Assembly assembly = TryGetExternalAssembly("avilmask.nonunopinata", "NonUnoPinata");
		if (assembly != null)
		{
			NonUnoPinataIntegration.NUPAss = assembly;
			NUPActive = true;
			NonUnoPinataIntegration.DoPatch();
		}
		Assembly assembly2 = TryGetExternalAssembly("wiri.compositableloadouts", "Inventory");
		if (assembly2 != null)
		{
			CompositableLoadoutsIntegration.CLAss = assembly2;
			CLActive = true;
			CompositableLoadoutsIntegration.DoPatch();
		}
		Assembly assembly3 = TryGetExternalAssembly("dawnsglow.qualcolor", "QualityColors");
		if (assembly3 != null)
		{
			QualityColorsIntegration.QCAss = assembly3;
			QCActive = true;
		}
	}
}
