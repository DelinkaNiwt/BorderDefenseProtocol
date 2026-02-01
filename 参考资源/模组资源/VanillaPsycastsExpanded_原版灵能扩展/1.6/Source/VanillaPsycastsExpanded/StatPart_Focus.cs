using RimWorld;
using Verse;

namespace VanillaPsycastsExpanded;

public abstract class StatPart_Focus : StatPart
{
	public MeditationFocusDef focus;

	public bool ApplyOn(StatRequest req)
	{
		if (req.Thing is Pawn p && focus.CanPawnUse(p))
		{
			return StatPart_NearbyFoci.ShouldApply;
		}
		return false;
	}
}
