using System.Reflection;
using HarmonyLib;
using Verse;

namespace NCL_Storyteller;

[StaticConstructorOnStartup]
public class PatchMain
{
	public static Harmony instance;

	static PatchMain()
	{
		instance = new Harmony("Azzy_NCL_Storyteller");
		instance.PatchAll(Assembly.GetExecutingAssembly());
	}
}
