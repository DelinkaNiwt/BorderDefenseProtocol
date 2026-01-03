using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompProperties_AbilityDamageBuildingArea : CompProperties_AbilityEffect
{
	public float radius = 2f;

	public int damageAmountBase = 10;

	public bool applyOnAlly = true;

	public DamageDef damageDef = DamageDefOf.Blunt;

	public FleckDef fleckOnCell;

	public EffecterDef effecter;

	public bool targetOnCaster = false;

	public CompProperties_AbilityDamageBuildingArea()
	{
		compClass = typeof(CompAbilityDamageBuildingArea);
	}
}
