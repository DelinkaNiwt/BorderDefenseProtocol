using RimWorld;
using Verse;

namespace NCL;

public class Comp_SmokeTrail : ThingComp
{
	private int ticksCounter;

	public CompProperties_SmokeTrail Props => (CompProperties_SmokeTrail)props;

	public override void CompTick()
	{
		base.CompTick();
		ticksCounter++;
		if (ticksCounter >= Props.intervalTicks)
		{
			ticksCounter = 0;
			if (parent.Map != null && parent.Position.IsValid)
			{
				FleckMaker.ThrowSmoke(parent.DrawPos, parent.Map, Props.smokeSize);
			}
		}
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref ticksCounter, "ticksCounter", 0);
	}
}
