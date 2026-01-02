using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompProperties_AbilityShiftForward : CompProperties_AbilityEffect
{
	public float distance = 5f;

	public FleckDef shiftFleck;

	public CompProperties_AbilityShiftForward()
	{
		compClass = typeof(CompAbilityShiftForward);
	}
}
