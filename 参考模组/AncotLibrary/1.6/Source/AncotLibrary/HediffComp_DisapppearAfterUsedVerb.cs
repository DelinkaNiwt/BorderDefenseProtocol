using Verse;

namespace AncotLibrary;

public class HediffComp_DisapppearAfterUsedVerb : HediffComp
{
	private bool canDisappear = false;

	private bool disappearInTime = false;

	private int ticks = 0;

	private HediffCompProperties_DisapppearAfterUsedVerb Props => (HediffCompProperties_DisapppearAfterUsedVerb)props;

	public override void CompPostTickInterval(ref float severityAdjustment, int delta)
	{
		base.CompPostTickInterval(ref severityAdjustment, delta);
		ticks += delta;
		if (ticks > 10)
		{
			canDisappear = true;
		}
		if (disappearInTime && ticks > Props.delayTicks)
		{
			if (Props.disapppearEffecter != null)
			{
				Effecter effecter = new Effecter(Props.disapppearEffecter);
				effecter.Trigger(base.Pawn, TargetInfo.Invalid).Cleanup();
			}
			parent.Severity -= parent.Severity;
		}
	}

	public override void Notify_PawnUsedVerb(Verb verb, LocalTargetInfo target)
	{
		if (canDisappear)
		{
			disappearInTime = true;
			ticks = 0;
		}
	}

	public override void CompExposeData()
	{
		Scribe_Values.Look(ref canDisappear, "canDisappear", defaultValue: false);
		Scribe_Values.Look(ref disappearInTime, "disappearInTime", defaultValue: false);
		Scribe_Values.Look(ref ticks, "ticks", 0);
	}
}
