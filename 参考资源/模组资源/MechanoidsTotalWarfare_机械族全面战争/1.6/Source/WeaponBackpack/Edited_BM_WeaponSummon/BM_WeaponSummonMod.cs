using HarmonyLib;
using Verse;

namespace Edited_BM_WeaponSummon;

public class BM_WeaponSummonMod : Mod
{
	public BM_WeaponSummonMod(ModContentPack pack)
		: base(pack)
	{
		new Harmony("BM_WeaponSummonMod").PatchAll();
	}
}
