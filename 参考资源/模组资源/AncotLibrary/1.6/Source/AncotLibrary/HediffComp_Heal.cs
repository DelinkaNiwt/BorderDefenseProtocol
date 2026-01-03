using Verse;

namespace AncotLibrary;

public class HediffComp_Heal : HediffComp
{
	private HediffCompProperties_Heal Props => (HediffCompProperties_Heal)props;

	public override void CompPostTickInterval(ref float severityAdjustment, int delta)
	{
		base.CompPostTickInterval(ref severityAdjustment, delta);
		if (base.Pawn.IsHashIntervalTick(Props.intervalTicks, delta))
		{
			if (Props.effectDef != null)
			{
				Effecter effecter = Props.effectDef.Spawn();
				effecter.Trigger(new TargetInfo(base.Pawn.Position, base.Pawn.Map), null);
				effecter.Cleanup();
			}
			RepairTick(base.Pawn, 1);
		}
	}

	public static void RepairTick(Pawn pawn, int delta)
	{
		GetHediffToHeal(pawn)?.Heal(delta);
	}

	private static Hediff GetHediffToHeal(Pawn pawn)
	{
		Hediff hediff = null;
		float num = float.PositiveInfinity;
		foreach (Hediff hediff2 in pawn.health.hediffSet.hediffs)
		{
			if (hediff2 is Hediff_Injury && hediff2.Severity < num)
			{
				num = hediff2.Severity;
				hediff = hediff2;
			}
		}
		if (hediff != null)
		{
			return hediff;
		}
		return null;
	}
}
