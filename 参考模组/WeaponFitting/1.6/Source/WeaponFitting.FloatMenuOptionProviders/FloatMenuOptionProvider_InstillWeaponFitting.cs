using System.Collections.Generic;
using AncotLibrary;
using RimWorld;
using Verse;
using Verse.AI;

namespace WeaponFitting.FloatMenuOptionProviders;

public class FloatMenuOptionProvider_InstillWeaponFitting : FloatMenuOptionProvider
{
	protected override bool Drafted => true;

	protected override bool Undrafted => true;

	protected override bool Multiselect => false;

	protected override bool MechanoidCanDo => true;

	public override bool Applies(FloatMenuContext context)
	{
		return !context.IsMultiselect && context.FirstSelectedPawn != null;
	}

	protected override FloatMenuOption GetSingleOptionFor(Thing clickedThing, FloatMenuContext context)
	{
		CompWeaponFitting comp = clickedThing?.TryGetComp<CompWeaponFitting>();
		if (comp == null || comp.Props.trait == null)
		{
			return null;
		}
		Pawn selPawn = context.FirstSelectedPawn;
		ThingWithComps weapon = selPawn.equipment?.Primary;
		List<WeaponTraitDef> replacedTraits;
		AcceptanceReport report = WeaponTraitsUtility.CanAddTraits(comp.Props.trait, weapon, out replacedTraits);
		string label = "Ancot.WeaponFitting_Assemble".Translate(clickedThing);
		FloatMenuOption floatMenuOption = new FloatMenuOption(label, delegate
		{
			if (!replacedTraits.NullOrEmpty())
			{
				string text = replacedTraits[0].label;
				if (replacedTraits.Count > 1)
				{
					for (int i = 1; i < replacedTraits.Count; i++)
					{
						text = text + "," + replacedTraits[i].label;
					}
				}
				Messages.Message("Ancot.WeaponFitting_ReplceTrait".Translate(comp.Props.trait.label, text, weapon.LabelCap), MessageTypeDefOf.RejectInput, historical: false);
			}
			selPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(AncotJobDefOf.Ancot_UseWeaponFittingSelf, clickedThing, weapon), JobTag.Misc);
		});
		if ((bool)report)
		{
			floatMenuOption.Disabled = false;
		}
		else if (report.Reason != null)
		{
			floatMenuOption.Label = "Ancot.WeaponFitting_NotAvailable".Translate(clickedThing) + "(" + report.Reason + ")";
			floatMenuOption.Disabled = true;
		}
		else
		{
			floatMenuOption.Label = "Ancot.WeaponFitting_NotAvailable".Translate(clickedThing);
			floatMenuOption.Disabled = true;
		}
		return floatMenuOption;
	}
}
