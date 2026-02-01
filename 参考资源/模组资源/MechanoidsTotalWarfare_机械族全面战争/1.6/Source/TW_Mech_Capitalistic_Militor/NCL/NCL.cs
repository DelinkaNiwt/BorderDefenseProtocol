using HarmonyLib;
using Verse;

namespace NCL;

[StaticConstructorOnStartup]
public class NCL : Mod
{
	static NCL()
	{
		Harmony harmony = new Harmony("NCL.MechanoidTrader");
		harmony.PatchAll();
	}

	public NCL(ModContentPack content)
		: base(content)
	{
	}
}
