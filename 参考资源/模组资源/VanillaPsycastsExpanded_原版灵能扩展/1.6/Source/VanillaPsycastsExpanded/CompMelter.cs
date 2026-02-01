using RimWorld;
using Verse;

namespace VanillaPsycastsExpanded;

public class CompMelter : ThingComp
{
	public float damageBuffer;

	public override void CompTick()
	{
		base.CompTick();
		if (!parent.IsHashIntervalTick(60))
		{
			return;
		}
		float ambientTemperature = parent.AmbientTemperature;
		if (ambientTemperature > 0f)
		{
			damageBuffer += ambientTemperature / 41.66f;
			if (damageBuffer >= 1f)
			{
				parent.HitPoints -= (int)damageBuffer;
				damageBuffer = 0f;
			}
			if (parent.HitPoints < 0)
			{
				FilthMaker.TryMakeFilth(parent.Position, parent.Map, ThingDefOf.Filth_Water);
				parent.Destroy();
			}
		}
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref damageBuffer, "damageBuffer", 0f);
	}
}
