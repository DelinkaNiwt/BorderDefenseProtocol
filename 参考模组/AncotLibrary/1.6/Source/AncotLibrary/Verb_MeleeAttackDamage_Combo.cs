using Verse;

namespace AncotLibrary;

public class Verb_MeleeAttackDamage_Combo : Verb_MeleeAttackDamage_Effecter
{
	private const float MeleeDamageRandomFactorMin = 0.8f;

	private const float MeleeDamageRandomFactorMax = 1.2f;

	public CompMeleeCombo CompMeleeCombo => base.EquipmentSource.TryGetComp<CompMeleeCombo>();

	public override void WarmupComplete()
	{
		base.WarmupComplete();
		CompMeleeCombo?.TryComboOnce();
	}
}
