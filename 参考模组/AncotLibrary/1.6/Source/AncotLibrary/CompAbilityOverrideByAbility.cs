using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompAbilityOverrideByAbility : CompAbilityEffect
{
	public new CompProperties_AbilityOverrideByAbility Props => (CompProperties_AbilityOverrideByAbility)props;

	private Pawn Caster => parent.pawn;

	private bool AbilityDisabled => Caster.abilities.AllAbilitiesForReading.ContainsAny((Ability a) => Props.abilities.Contains(a.def));

	public override bool ShouldHideGizmo => AbilityDisabled;

	public override bool CanCast => !AbilityDisabled;

	public override bool GizmoDisabled(out string reason)
	{
		if (AbilityDisabled)
		{
			reason = "".Translate();
			return true;
		}
		reason = "";
		return false;
	}
}
