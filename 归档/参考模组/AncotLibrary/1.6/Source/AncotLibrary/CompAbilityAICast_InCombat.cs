using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompAbilityAICast_InCombat : CompAbilityEffect
{
	private new CompProperties_AICast_InCombat Props => (CompProperties_AICast_InCombat)props;

	public bool autoFightForPlayer
	{
		get
		{
			CompMechAutoFight compMechAutoFight = Caster.TryGetComp<CompMechAutoFight>();
			if (compMechAutoFight != null && Caster != null && Caster.Faction.IsPlayer)
			{
				return compMechAutoFight.AutoFight;
			}
			return false;
		}
	}

	public Pawn Caster => parent.pawn;

	public override bool AICanTargetNow(LocalTargetInfo target)
	{
		if (Caster.InAggroMentalState || autoFightForPlayer)
		{
			return true;
		}
		return false;
	}
}
