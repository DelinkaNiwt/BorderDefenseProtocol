using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public class FloatMenuOptionProvider_Drone : FloatMenuOptionProvider
{
	protected override bool Drafted => true;

	protected override bool Undrafted => true;

	protected override bool Multiselect => false;

	protected override bool RequiresManipulation => true;

	protected override bool AppliesInt(FloatMenuContext context)
	{
		return context.FirstSelectedPawn.Faction.IsPlayerSafe();
	}

	public override IEnumerable<FloatMenuOption> GetOptionsFor(Pawn clickedPawn, FloatMenuContext context)
	{
		if (clickedPawn.TryGetComp<CompDrone>() == null || clickedPawn.Drafted)
		{
			yield break;
		}
		yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("DisassembleMech".Translate(clickedPawn.LabelCap), delegate
		{
			WindowStack windowStack = Find.WindowStack;
			TaggedString text = "ConfirmDisassemblingMech".Translate(clickedPawn.LabelCap) + ":\n" + (from x in MechanitorUtility.IngredientsFromDisassembly(clickedPawn.def)
				select x.Summary).ToLineList("  - ");
			Action confirmedAct = delegate
			{
				context.FirstSelectedPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.DisassembleMech, clickedPawn), JobTag.Misc);
			};
			windowStack.Add(Dialog_MessageBox.CreateConfirmation(text, confirmedAct, destructive: true));
		}, MenuOptionPriority.Low, null, null, 0f, null, null, playSelectionSound: true, -20), context.FirstSelectedPawn, new LocalTargetInfo(clickedPawn));
	}
}
