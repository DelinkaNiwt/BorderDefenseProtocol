using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace NiceInventoryTab;

public class JobDriver_RemoveApparelToInventory : JobDriver
{
	private int duration;

	private Apparel Apparel => (Apparel)job.GetTarget(TargetIndex.A).Thing;

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		return true;
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref duration, "duration", 0);
	}

	public override void Notify_Starting()
	{
		base.Notify_Starting();
		duration = (int)(Apparel.GetStatValue(StatDefOf.EquipDelay) * 60f);
	}

	public bool MoveToInventory()
	{
		if (!CommandUtility.CanFitInInventory(pawn, Apparel.def, out var _, ignoreEquipment: true))
		{
			return false;
		}
		return true;
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		this.FailOnDestroyedOrNull(TargetIndex.A);
		yield return Toils_General.Wait(duration).PlaySustainerOrSound(Apparel.def.apparel.soundRemove).WithProgressBarToilDelay(TargetIndex.A);
		yield return Toils_General.Do(delegate
		{
			if (!pawn.apparel.WornApparel.Contains(Apparel))
			{
				EndJobWith(JobCondition.Incompletable);
			}
			else if (!MoveToInventory())
			{
				EndJobWith(JobCondition.Incompletable);
			}
			else
			{
				pawn.apparel.Remove(Apparel);
				pawn.inventory.innerContainer.TryAdd(Apparel, canMergeWithExistingStacks: false);
				EndJobWith(JobCondition.Succeeded);
			}
		});
	}
}
