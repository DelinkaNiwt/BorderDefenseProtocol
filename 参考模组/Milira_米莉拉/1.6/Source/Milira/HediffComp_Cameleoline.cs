using Verse;

namespace Milira;

public class HediffComp_Cameleoline : HediffComp
{
	private bool flag;

	private float ticks;

	private HediffCompProperties_Cameleoline Props => (HediffCompProperties_Cameleoline)props;

	public override void Notify_PawnUsedVerb(Verb verb, LocalTargetInfo target)
	{
		ticks = 0f;
		flag = true;
	}

	public override void CompPostTick(ref float severityAdjustment)
	{
		if (!flag)
		{
			return;
		}
		ticks += 1f;
		if (ticks > Props.recoverTick)
		{
			base.Pawn.health.AddHediff(MiliraDefOf.Milian_Cameleoline);
			if (Props.effecter != null)
			{
				Effecter effecter = new Effecter(Props.effecter);
				effecter.Trigger(base.Pawn, TargetInfo.Invalid).Cleanup();
			}
			flag = false;
		}
	}

	public override void CompExposeData()
	{
		Scribe_Values.Look(ref flag, "flag", defaultValue: false);
		Scribe_Values.Look(ref ticks, "ticks", 0f);
	}
}
