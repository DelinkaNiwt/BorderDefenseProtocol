using RimWorld;
using Verse;

namespace VanillaPsycastsExpanded;

public class HediffComp_SpawnMote : HediffComp
{
	public Mote spawnedMote;

	public HediffCompProperties_SpawnMote Props => props as HediffCompProperties_SpawnMote;

	public override void CompPostTick(ref float severityAdjustment)
	{
		if (spawnedMote == null)
		{
			spawnedMote = MoteMaker.MakeAttachedOverlay(base.Pawn, Props.moteDef, Props.offset);
			if (spawnedMote is MoteAttachedScaled moteAttachedScaled)
			{
				moteAttachedScaled.maxScale = Props.maxScale;
			}
		}
		if (spawnedMote.def.mote.needsMaintenance)
		{
			spawnedMote.Maintain();
		}
		base.CompPostTick(ref severityAdjustment);
	}

	public override void CompExposeData()
	{
		base.CompExposeData();
		Scribe_References.Look(ref spawnedMote, "spawnedMote");
	}
}
