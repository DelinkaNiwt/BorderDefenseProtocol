using System.Reflection;
using HarmonyLib;
using Verse;

namespace NoBody;

internal class HarmonyInit
{
	[StaticConstructorOnStartup]
	public class PatchMain
	{
		public static Harmony instance;

		static PatchMain()
		{
			instance = new Harmony("NoBody.Harmony");
			instance.PatchAll(Assembly.GetExecutingAssembly());
		}
	}
}
