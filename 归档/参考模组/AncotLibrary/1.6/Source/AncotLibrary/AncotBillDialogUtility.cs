using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace AncotLibrary;

public static class AncotBillDialogUtility
{
	public static IEnumerable<Widgets.DropdownMenuElement<Pawn>> GetMechRestrictionOptionsForBill(Bill bill, Func<Pawn, bool> pawnValidator = null)
	{
		bool workSkill = bill.recipe.workSkill != null;
		IEnumerable<Pawn> enumerable = bill.Map.mapPawns.SpawnedColonyMechs;
		enumerable = from pawn2 in enumerable
			where pawnValidator == null || pawnValidator(pawn2)
			orderby pawn2.LabelShortCap
			select pawn2;
		if (workSkill)
		{
			enumerable = enumerable.OrderByDescending((Pawn pawn2) => pawn2.RaceProps.mechFixedSkillLevel);
		}
		WorkGiverDef workGiver = bill.billStack.billGiver.GetWorkgiver();
		if (workGiver == null)
		{
			Log.ErrorOnce("Generating pawn restrictions for a BillGiver without a Workgiver", 96455148);
			yield break;
		}
		enumerable = enumerable.OrderByDescending((Pawn pawn2) => pawn2.workSettings.WorkIsActive(workGiver.workType));
		enumerable = enumerable.OrderBy((Pawn pawn2) => pawn2.WorkTypeIsDisabled(workGiver.workType));
		foreach (Pawn pawn in enumerable)
		{
			if (bill.recipe.workSkill != null && !pawn.workSettings.WorkIsActive(workGiver.workType))
			{
				yield return new Widgets.DropdownMenuElement<Pawn>
				{
					option = new FloatMenuOption(string.Format("{0} ({1} {2}, {3})", pawn.LabelShortCap, pawn.RaceProps.mechFixedSkillLevel, bill.recipe.workSkill.label, "NotAssigned".Translate()), delegate
					{
						bill.SetPawnRestriction(pawn);
					}),
					payload = pawn
				};
			}
			else if (!pawn.workSettings.WorkIsActive(workGiver.workType))
			{
				yield return new Widgets.DropdownMenuElement<Pawn>
				{
					option = new FloatMenuOption(string.Format("{0} ({1})", pawn.LabelShortCap, "NotAssigned".Translate()), delegate
					{
						bill.SetPawnRestriction(pawn);
					}),
					payload = pawn
				};
			}
			else if (bill.recipe.workSkill != null)
			{
				yield return new Widgets.DropdownMenuElement<Pawn>
				{
					option = new FloatMenuOption($"{pawn.LabelShortCap} ({pawn.RaceProps.mechFixedSkillLevel} {bill.recipe.workSkill.label})", delegate
					{
						bill.SetPawnRestriction(pawn);
					}),
					payload = pawn
				};
			}
			else
			{
				yield return new Widgets.DropdownMenuElement<Pawn>
				{
					option = new FloatMenuOption($"{pawn.LabelShortCap}", delegate
					{
						bill.SetPawnRestriction(pawn);
					}),
					payload = pawn
				};
			}
		}
	}
}
