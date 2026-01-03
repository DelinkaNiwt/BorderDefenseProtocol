using Verse;

namespace AncotLibrary;

public class HediffComp_SeverityChangedByVacuum : HediffComp
{
	public HediffCompProperties_SeverityChangeByVacuum Props => (HediffCompProperties_SeverityChangeByVacuum)props;

	public float Vacuum_D => Props.standardVacuum - base.Pawn.Position.GetVacuum(base.Pawn.MapHeld);

	public override void CompPostTickInterval(ref float severityAdjustment, int delta)
	{
		base.CompPostTickInterval(ref severityAdjustment, delta);
		if (ModsConfig.OdysseyActive && base.Pawn.Spawned)
		{
			if (!base.Pawn.Map.Biome.inVacuum)
			{
				severityAdjustment += Props.severityPerTick_High * (float)delta;
			}
			if (Vacuum_D > 0f)
			{
				severityAdjustment += Vacuum_D * Props.severityPerTick_High * (float)delta;
			}
			if (Vacuum_D < 0f)
			{
				severityAdjustment += Vacuum_D * Props.severityPerTick_Low * (float)delta;
			}
		}
	}
}
