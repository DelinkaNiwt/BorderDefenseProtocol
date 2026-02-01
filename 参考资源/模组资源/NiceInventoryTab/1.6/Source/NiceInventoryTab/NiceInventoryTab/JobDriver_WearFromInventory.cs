using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace NiceInventoryTab;

public class JobDriver_WearFromInventory : JobDriver
{
	private int duration;

	private Apparel Apparel => (Apparel)job.GetTarget(TargetIndex.A).Thing;

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref duration, "duration", 0);
	}

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		return true;
	}

	public override void Notify_Starting()
	{
		base.Notify_Starting();
		duration = (int)(Apparel.GetStatValue(StatDefOf.EquipDelay) * 60f);
	}

	public bool WearFromInventory()
	{
		Apparel apparel = Apparel;
		List<Apparel> wornApparel = pawn.apparel.WornApparel;
		for (int num = wornApparel.Count - 1; num >= 0; num--)
		{
			if (!ApparelUtility.CanWearTogether(apparel.def, wornApparel[num].def, pawn.RaceProps.body))
			{
				return false;
			}
		}
		return true;
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		this.FailOnDestroyedOrNull(TargetIndex.A);
		yield return Toils_General.Wait(duration).PlaySustainerOrSound(Apparel.def.apparel.soundRemove).WithProgressBarToilDelay(TargetIndex.A);
		yield return Toils_General.Do(delegate
		{
			if (!pawn.inventory.innerContainer.Contains(Apparel))
			{
				EndJobWith(JobCondition.Incompletable);
			}
			else if (!WearFromInventory())
			{
				EndJobWith(JobCondition.Incompletable);
			}
			else
			{
				pawn.inventory.innerContainer.Remove(Apparel);
				pawn.apparel.Wear(Apparel);
				EndJobWith(JobCondition.Succeeded);
			}
		});
	}
}
