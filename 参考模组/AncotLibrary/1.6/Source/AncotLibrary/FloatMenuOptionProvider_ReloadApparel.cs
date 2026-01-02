using System.Collections.Generic;
using RimWorld;
using RimWorld.Utility;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public class FloatMenuOptionProvider_ReloadApparel : FloatMenuOptionProvider
{
	protected override bool Drafted => true;

	protected override bool Undrafted => true;

	protected override bool Multiselect => false;

	public override bool Applies(FloatMenuContext context)
	{
		return !context.IsMultiselect && context.FirstSelectedPawn != null;
	}

	public override IEnumerable<FloatMenuOption> GetOptions(FloatMenuContext context)
	{
		Pawn pawn = context.FirstSelectedPawn;
		Map map = context.map;
		IntVec3 clickCell = context.ClickedCell;
		if (!clickCell.InBounds(map))
		{
			yield break;
		}
		foreach (Pair<IReloadableComp, Thing> pair in ReloadableUtilityCustom.FindPotentiallyReloadableGear(pawn, clickCell.GetThingList(map)))
		{
			IReloadableComp reloadable = pair.First;
			Thing gear = pair.Second;
			ThingComp comp = reloadable as ThingComp;
			TaggedString label = "Reload".Translate(comp.parent.Named("GEAR"), NamedArgumentUtility.Named(reloadable.AmmoDef, "AMMO")) + (" (" + reloadable.LabelRemaining + ")");
			if (!pawn.CanReach(gear, PathEndMode.ClosestTouch, Danger.Deadly))
			{
				yield return new FloatMenuOption(label + ": " + "NoPath".Translate().CapitalizeFirst(), null);
				continue;
			}
			if (!reloadable.NeedsReload(allowForceReload: true))
			{
				yield return new FloatMenuOption(label + ": " + "ReloadFull".Translate(), null);
				continue;
			}
			List<Thing> ammo = ReloadableUtilityCustom.FindEnoughAmmo(pawn, gear.Position, reloadable, forceReload: true);
			if (ammo == null)
			{
				yield return new FloatMenuOption(label + ": " + "ReloadNotEnough".Translate(), null);
				continue;
			}
			if (pawn.carryTracker.AvailableStackSpace(reloadable.AmmoDef) < reloadable.MinAmmoNeeded(allowForcedReload: true))
			{
				yield return new FloatMenuOption(label + ": " + "ReloadCannotCarryEnough".Translate(NamedArgumentUtility.Named(reloadable.AmmoDef, "AMMO")), null);
				continue;
			}
			yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(action: delegate
			{
				pawn.jobs.TryTakeOrderedJob(JobGiver_Reload_ApparelReloadable.MakeReloadJob(reloadable, ammo), JobTag.Misc);
			}, label: label), pawn, gear);
		}
	}
}
