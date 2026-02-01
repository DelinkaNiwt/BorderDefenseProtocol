using System;
using HarmonyLib;
using Verse;

namespace NCL;

[StaticConstructorOnStartup]
public static class NCL_Initializer
{
	static NCL_Initializer()
	{
		try
		{
			Log.Message("NCL Mod: Applying Harmony patches");
			if (NCL_Mod.harmony == null)
			{
				NCL_Mod.harmony = new Harmony("com.yourname.NCL");
				Log.Message("NCL Mod: Harmony instance created in static initializer");
			}
			Type patchType = typeof(Patch_CommanderControlRange);
			NCL_Mod.harmony.CreateClassProcessor(patchType).Patch();
			Log.Message("NCL Mod: Applied CommanderControlRange patch");
		}
		catch (Exception arg)
		{
			Log.Error($"NCL Mod: Failed to apply Harmony patches: {arg}");
		}
	}
}
