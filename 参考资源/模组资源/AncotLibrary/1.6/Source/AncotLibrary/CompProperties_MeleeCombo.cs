using Verse;

namespace AncotLibrary;

public class CompProperties_MeleeCombo : CompProperties
{
	public float comboChance = 0.1f;

	public int maxCombo = 5;

	public int comboStanceTick = 5;

	public bool useWeaponCharge = false;

	public CompProperties_MeleeCombo()
	{
		compClass = typeof(CompMeleeCombo);
	}
}
