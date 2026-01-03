using AncotLibrary;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace WeaponFitting;

[HarmonyPatch(typeof(MainTabWindow_Inspect))]
[HarmonyPatch("DoInspectPaneButtons")]
public static class MainTabWindow_Inspect_Patch
{
	[HarmonyPostfix]
	public static void Postfix(Rect rect, ref float lineEndWidth)
	{
		if (Find.Selector.NumSelected == 1)
		{
			Thing singleSelectedThing = Find.Selector.SingleSelectedThing;
			CompUniqueWeapon comp = singleSelectedThing.TryGetComp<CompUniqueWeapon>();
			if (comp != null && !comp.IsTraitsEmpty())
			{
				float x = rect.width - 78f;
				WF_Utility.DrawRenameButton(new Rect(x, 0f, 30f, 30f), singleSelectedThing);
				lineEndWidth += 30f;
			}
		}
	}
}
