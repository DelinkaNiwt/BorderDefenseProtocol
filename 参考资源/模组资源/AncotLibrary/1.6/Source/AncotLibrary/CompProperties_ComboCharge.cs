using RimWorld;

namespace AncotLibrary;

public class CompProperties_ComboCharge : CompProperties_AbilityEffect
{
	public int comboTick = 240;

	public float? comboWarmupProgressPct;

	public CompProperties_ComboCharge()
	{
		compClass = typeof(CompAbilityComboCharge);
	}
}
