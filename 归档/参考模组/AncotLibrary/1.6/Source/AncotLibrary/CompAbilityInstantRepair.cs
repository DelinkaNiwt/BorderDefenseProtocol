using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompAbilityInstantRepair : CompAbilityEffect
{
	public new CompProperties_AbilityInstantRepair Props => (CompProperties_AbilityInstantRepair)props;

	public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
	{
		if (target.Pawn == null)
		{
			return;
		}
		if (Props.effecter != null)
		{
			Effecter effecter = Props.effecter.Spawn();
			effecter.Trigger(new TargetInfo(target.Cell, target.Pawn.Map), null);
			effecter.Cleanup();
		}
		for (int i = 0; i < Props.repairPoint; i++)
		{
			if (MechRepairUtility.CanRepair(target.Pawn))
			{
				MechRepairUtility.RepairTick(target.Pawn, 1);
			}
		}
	}
}
