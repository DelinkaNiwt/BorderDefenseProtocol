using RimWorld;
using Verse;

namespace AncotLibrary;

public class HediffComp_MechRepair : HediffComp
{
	private HediffCompProperties_MechRepair Props => (HediffCompProperties_MechRepair)props;

	public override void CompPostTickInterval(ref float severityAdjustment, int delta)
	{
		base.CompPostTickInterval(ref severityAdjustment, delta);
		if (base.Pawn.IsHashIntervalTick(Props.intervalTicks, delta) && MechRepairUtility.CanRepair(base.Pawn) && (base.Pawn.needs.energy == null || base.Pawn.needs.energy.CurLevelPercentage > Props.energyThreshold))
		{
			if (Props.effectDef != null)
			{
				Effecter effecter = Props.effectDef.Spawn();
				effecter.Trigger(new TargetInfo(base.Pawn.Position, base.Pawn.Map), null);
				effecter.Cleanup();
			}
			MechRepairUtility.RepairTick(base.Pawn, 1);
			if (base.Pawn.needs.energy != null)
			{
				base.Pawn.needs.energy.CurLevelPercentage -= Props.energyPctPerRepair;
			}
		}
	}
}
