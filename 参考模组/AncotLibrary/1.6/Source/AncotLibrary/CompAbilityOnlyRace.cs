using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompAbilityOnlyRace : CompAbilityEffect
{
	public new CompProperties_AbilityOnlyRace Props => (CompProperties_AbilityOnlyRace)props;

	private Pawn Pawn => parent.pawn;

	public override bool AICanTargetNow(LocalTargetInfo target)
	{
		return Props.races.Contains(Pawn.def.defName);
	}

	public override bool GizmoDisabled(out string reason)
	{
		if (!Props.races.Contains(Pawn.def.defName))
		{
			reason = "Ancot.Ability_OnlyRace".Translate();
			return true;
		}
		reason = "";
		return false;
	}
}
