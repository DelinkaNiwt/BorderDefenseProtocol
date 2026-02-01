using RimWorld.Planet;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded;

public class Ability_StealVitality : Ability
{
	public override void Cast(params GlobalTargetInfo[] targets)
	{
		((Ability)this).Cast(targets);
		((Ability)this).ApplyHediff(base.pawn, VPE_DefOf.VPE_GainedVitality, (BodyPartRecord)null, ((Ability)this).GetDurationForPawn(), 0f);
	}
}
