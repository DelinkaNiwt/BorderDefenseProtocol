using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompProperties_AbilityStunArea : CompProperties_AbilityEffect
{
	public float radius = 2f;

	public int stunForTicks = 10;

	public bool applyOnAlly = true;

	public bool applyOnAllyOnly = false;

	public bool applyOnMech = true;

	public bool ignoreCaster = false;

	public bool targetOnCaster = false;

	public EffecterDef effecter;

	public CompProperties_AbilityStunArea()
	{
		compClass = typeof(CompAbilityStunArea);
	}
}
