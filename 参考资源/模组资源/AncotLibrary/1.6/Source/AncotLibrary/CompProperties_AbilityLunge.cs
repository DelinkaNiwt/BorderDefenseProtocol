using Verse;

namespace AncotLibrary;

public class CompProperties_AbilityLunge : CompProperties_AbilityShiftForward
{
	public bool applyOnAlly = true;

	public bool applyOnAllyOnly = false;

	public bool applyOnMech = true;

	public bool ignoreCaster = false;

	public int damageAmount = 5;

	public float armorPenetration = 1f;

	public DamageDef damageDef;

	public CompProperties_AbilityLunge()
	{
		compClass = typeof(CompAbilityLunge);
	}
}
