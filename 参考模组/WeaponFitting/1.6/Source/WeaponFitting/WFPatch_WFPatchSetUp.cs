using System.Reflection;
using HarmonyLib;
using Verse;

namespace WeaponFitting;

[StaticConstructorOnStartup]
public class WFPatch_WFPatchSetUp
{
	static WFPatch_WFPatchSetUp()
	{
		Harmony harmony = new Harmony("FatCat.WeaponFittingPatchSetUp");
		harmony.PatchAll(Assembly.GetExecutingAssembly());
	}
}
