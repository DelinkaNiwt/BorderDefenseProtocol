using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompAbilityUseMechEnergy : CompAbilityEffect
{
	public new CompProperties_AbilityUseMechEnergy Props => (CompProperties_AbilityUseMechEnergy)props;

	private Pawn Pawn => parent.pawn;

	private Need_MechEnergy Energy => Pawn.needs.energy;

	public override bool AICanTargetNow(LocalTargetInfo target)
	{
		string reason;
		return !GizmoDisabled(out reason);
	}

	public override bool GizmoDisabled(out string reason)
	{
		if ((Energy != null && Energy.CurLevelPercentage < Props.energyPerUse) || !Props.canUseIfNoEnergyNeed)
		{
			reason = "Ancot.Ability_NoMechEnergy".Translate();
			return true;
		}
		reason = "";
		return false;
	}

	public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
	{
		base.Apply(target, dest);
		if (Energy != null)
		{
			Energy.CurLevelPercentage -= Props.energyPerUse;
		}
	}
}
