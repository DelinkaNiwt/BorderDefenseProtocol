using System.Collections.Generic;
using RimWorld;
using Verse;

namespace VanillaPsycastsExpanded.Nightstalker;

public class Hediff_ShadowFocus : HediffWithComps
{
	public override HediffStage CurStage => new HediffStage
	{
		statOffsets = new List<StatModifier>
		{
			new StatModifier
			{
				stat = StatDefOf.PsychicSensitivity,
				value = 1f - pawn.MapHeld.glowGrid.GroundGlowAt(pawn.PositionHeld)
			}
		}
	};
}
