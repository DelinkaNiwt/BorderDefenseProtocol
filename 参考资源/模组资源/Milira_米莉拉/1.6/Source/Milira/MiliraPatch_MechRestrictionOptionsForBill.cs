using System;
using System.Collections.Generic;
using AncotLibrary;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Milira;

[HarmonyPatch(typeof(Dialog_BillConfig), "GeneratePawnRestrictionOptions")]
public static class MiliraPatch_MechRestrictionOptionsForBill
{
	[HarmonyPostfix]
	public static IEnumerable<Widgets.DropdownMenuElement<Pawn>> Postfix(IEnumerable<Widgets.DropdownMenuElement<Pawn>> __result, Dialog_BillConfig __instance, Bill ___bill)
	{
		foreach (Widgets.DropdownMenuElement<Pawn> item in __result)
		{
			yield return item;
		}
		if (___bill.recipe.mechanitorOnlyRecipe)
		{
			yield break;
		}
		foreach (Widgets.DropdownMenuElement<Pawn> item2 in AncotBillDialogUtility.GetMechRestrictionOptionsForBill(___bill, (Func<Pawn, bool>)((Pawn p) => MilianUtility.IsMilian(p) && (p.TryGetComp<CompMilianApparelRender>()?.displayInBillWorker ?? false))))
		{
			yield return item2;
		}
	}
}
