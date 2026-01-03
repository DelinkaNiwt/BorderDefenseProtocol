using AncotLibrary;
using RimWorld;
using Verse;
using Verse.AI;

namespace WeaponFitting.FloatMenuOptionProviders;

public class FloatMenuOptionProvider_RemoveAllFitting : FloatMenuOptionProvider
{
	protected override bool Drafted => true;

	protected override bool MechanoidCanDo => true;

	protected override bool Undrafted => true;

	protected override bool Multiselect => false;

	protected override bool CanSelfTarget => true;

	protected virtual bool CanTargetOtherPawn => false;

	public override bool Applies(FloatMenuContext context)
	{
		return !context.IsMultiselect && context.FirstSelectedPawn != null;
	}

	protected override FloatMenuOption GetSingleOptionFor(Thing clickedThing, FloatMenuContext context)
	{
		if (!WF_DefOf.WeaponFittings_II.IsFinished)
		{
			return null;
		}
		Thing weapon = clickedThing;
		bool IsclickPawn = false;
		Pawn selPawn = context.FirstSelectedPawn;
		Pawn clickedPawn = clickedThing as Pawn;
		if (clickedPawn != null)
		{
			weapon = clickedPawn.equipment?.Primary;
			IsclickPawn = true;
		}
		CompUniqueWeapon comp = weapon?.TryGetComp<CompUniqueWeapon>();
		if (comp == null || comp.TraitsListForReading.NullOrEmpty() || (clickedPawn != null && clickedPawn != selPawn && !CanTargetOtherPawn))
		{
			return null;
		}
		if ((IsclickPawn && clickedPawn != selPawn && selPawn.CanReach(clickedPawn, PathEndMode.Touch, Danger.Deadly)) || !selPawn.CanReach(weapon, PathEndMode.ClosestTouch, Danger.Deadly))
		{
			return new FloatMenuOption("Ancot.RemoveWeaponFittings_NotAvailable".Translate(weapon.LabelCap) + ": " + "NoPath".Translate().CapitalizeFirst(), null);
		}
		return new FloatMenuOption("Ancot.RemoveAllFittings".Translate(weapon.LabelCap), delegate
		{
			weapon.SetForbidden(value: false, warnOnFail: false);
			if (!IsclickPawn)
			{
				selPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(AncotJobDefOf.Ancot_RemoveAllFittings, weapon), JobTag.Misc);
			}
			else
			{
				selPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(AncotJobDefOf.Ancot_RemoveAllFittings, clickedPawn, weapon), JobTag.Misc);
			}
		});
	}
}
