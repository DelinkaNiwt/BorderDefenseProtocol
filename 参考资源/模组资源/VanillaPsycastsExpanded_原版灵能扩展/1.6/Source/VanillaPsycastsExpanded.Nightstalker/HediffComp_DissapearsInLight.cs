using Verse;

namespace VanillaPsycastsExpanded.Nightstalker;

public class HediffComp_DissapearsInLight : HediffComp
{
	public override bool CompShouldRemove => (base.Pawn.MapHeld?.glowGrid?.GroundGlowAt(base.Pawn.PositionHeld)).GetValueOrDefault() >= 0.21f;
}
