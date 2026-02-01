using System;
using HarmonyLib;
using Verse;
using RimWorld;

namespace GD3
{
	[HarmonyPatch(typeof(Pawn), "Kill")]
	public static class BlackSuit_Patch
	{
		public static void Postfix(Pawn __instance)
		{
			if (__instance.def.defName == "Mech_Diabolus" && Find.World.GetComponent<MainComponent>().list_str.Find((string s) => s == "Mech_Diabolus") == null)
            {
				Find.World.GetComponent<MainComponent>().list_str.Add("Mech_Diabolus");
			}
			if (__instance.def.defName == "Mech_Warqueen" && Find.World.GetComponent<MainComponent>().list_str.Find((string s) => s == "Mech_Warqueen") == null)
			{
				Find.World.GetComponent<MainComponent>().list_str.Add("Mech_Warqueen");
			}
			if (__instance.def.defName == "Mech_Apocriton" && Find.World.GetComponent<MainComponent>().list_str.Find((string s) => s == "Mech_Apocriton") == null)
			{
				Find.World.GetComponent<MainComponent>().list_str.Add("Mech_Apocriton");
			}
		}
	}
}