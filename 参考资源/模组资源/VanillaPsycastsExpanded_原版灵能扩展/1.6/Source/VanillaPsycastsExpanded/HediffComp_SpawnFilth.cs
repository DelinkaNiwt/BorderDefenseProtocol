using RimWorld;
using Verse;

namespace VanillaPsycastsExpanded;

public class HediffComp_SpawnFilth : HediffComp
{
	public HediffCompProperties_SpawnFilth Props => props as HediffCompProperties_SpawnFilth;

	public override void CompPostTick(ref float severityAdjustment)
	{
		base.CompPostTick(ref severityAdjustment);
		if (base.Pawn.Spawned && base.Pawn.IsHashIntervalTick(Props.intervalRate))
		{
			FilthMaker.TryMakeFilth(base.Pawn.Position, base.Pawn.Map, Props.filthDef, Props.filthCount.RandomInRange);
		}
	}
}
