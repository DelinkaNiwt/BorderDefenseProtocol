using HarmonyLib;
using Verse;

namespace NCL;

public class NCL_Mod : Mod
{
	public static Harmony harmony;

	public NCL_Mod(ModContentPack content)
		: base(content)
	{
		Log.Message("NCL Mod: Initializing");
		if (harmony == null)
		{
			harmony = new Harmony("com.yourname.NCL");
			Log.Message("NCL Mod: Harmony instance created");
		}
	}
}
