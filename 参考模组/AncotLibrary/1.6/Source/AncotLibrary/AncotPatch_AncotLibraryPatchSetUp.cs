using System.Reflection;
using HarmonyLib;
using Verse;

namespace AncotLibrary;

[StaticConstructorOnStartup]
public class AncotPatch_AncotLibraryPatchSetUp
{
	static AncotPatch_AncotLibraryPatchSetUp()
	{
		Harmony harmony = new Harmony("Ancot.AncotPatch_AncotLibraryPatchSetUp");
		harmony.PatchAll(Assembly.GetExecutingAssembly());
	}
}
