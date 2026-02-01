using Verse;
using Verse.Sound;

namespace VanillaPsycastsExpanded;

public class HediffComp_PlaySound : HediffComp
{
	private Sustainer sustainer;

	public HediffCompProperties_PlaySound Props => (HediffCompProperties_PlaySound)props;

	public override void CompPostTick(ref float severityAdjustment)
	{
		base.CompPostTick(ref severityAdjustment);
		if (Props.sustainer != null)
		{
			if (sustainer == null || sustainer.Ended)
			{
				sustainer = Props.sustainer.TrySpawnSustainer(SoundInfo.InMap(base.Pawn, MaintenanceType.PerTick));
			}
			sustainer.Maintain();
		}
	}

	public override void CompPostPostRemoved()
	{
		base.CompPostPostRemoved();
		if (Props.sustainer != null && !sustainer.Ended)
		{
			sustainer?.End();
		}
		if (Props.endSound != null)
		{
			Props.endSound.PlayOneShot(base.Pawn);
		}
	}
}
