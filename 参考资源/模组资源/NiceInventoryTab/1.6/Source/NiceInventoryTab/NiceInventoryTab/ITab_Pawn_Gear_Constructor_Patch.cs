using HarmonyLib;
using RimWorld;
using UnityEngine;

namespace NiceInventoryTab;

[HarmonyPatch(typeof(ITab_Pawn_Gear))]
[HarmonyPatch(MethodType.Constructor)]
public static class ITab_Pawn_Gear_Constructor_Patch
{
	private static void Postfix(ITab_Pawn_Gear __instance, ref Vector2 ___size)
	{
		SetSize(ref ___size);
	}

	public static void SetSize(ref Vector2 size)
	{
		size = new Vector2(Settings.TabWidth, Settings.TabHeight);
	}

	public static void SetDefaultSize(ref Vector2 size)
	{
		size = new Vector2(460f, 450f);
	}
}
