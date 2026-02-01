using Verse;

namespace NCL;

public class HediffComp_SolarDisruption : HediffComp
{
	public HediffCompProperties_SolarDisruption Props => (HediffCompProperties_SolarDisruption)props;

	public override void CompPostTick(ref float severityAdjustment)
	{
		base.CompPostTick(ref severityAdjustment);
		if (parent?.pawn?.Map == null || !parent.pawn.Spawned)
		{
			return;
		}
		float glow = GetLocalGlow(parent.pawn.Map, parent.pawn.Position);
		if (glow < Props.sunlightThreshold)
		{
			if (!parent.pawn.health.hediffSet.HasHediff(Props.solarDisruptionHediff))
			{
				parent.pawn.health.AddHediff(Props.solarDisruptionHediff);
			}
		}
		else
		{
			Hediff existingHediff = parent.pawn.health.hediffSet.GetFirstHediffOfDef(Props.solarDisruptionHediff);
			existingHediff?.pawn.health.RemoveHediff(existingHediff);
		}
	}

	private float GetLocalGlow(Map map, IntVec3 position)
	{
		if (!position.InBounds(map))
		{
			return 0f;
		}
		return map.glowGrid.GroundGlowAt(position);
	}
}
