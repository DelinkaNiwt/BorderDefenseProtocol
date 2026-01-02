using AncotLibrary;
using RimWorld;
using Verse;
using Verse.AI;

namespace WeaponFitting;

public class FloatMenuOptionProvider_DisassembleWeaponFitting : FloatMenuOptionProvider
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
		CompUniqueWeapon comp = clickedThing?.TryGetComp<CompUniqueWeapon>();
		if (comp == null || comp.TraitsListForReading.NullOrEmpty())
		{
			return null;
		}
		CompEmptyUniqueWeapon comp2 = clickedThing.TryGetComp<CompEmptyUniqueWeapon>();
		if (comp2 != null)
		{
			return null;
		}
		if (!context.FirstSelectedPawn.CanReach(clickedThing, PathEndMode.ClosestTouch, Danger.Deadly))
		{
			return new FloatMenuOption("Ancot.DisassembleWeapon_NotAvailable".Translate(clickedThing.LabelCap, clickedThing) + ": " + "NoPath".Translate().CapitalizeFirst(), null);
		}
		return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("Ancot.DisassembleWeaponForFitting".Translate(clickedThing.LabelCap), delegate
		{
			Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("Ancot.DisassembleWeapon_Confirm".Translate(clickedThing.LabelCap), delegate
			{
				clickedThing.SetForbidden(value: false, warnOnFail: false);
				context.FirstSelectedPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(AncotJobDefOf.Ancot_DisassembleWeaponForFitting, clickedThing), JobTag.Misc);
			}, destructive: true));
		}), context.FirstSelectedPawn, clickedThing);
	}
}
