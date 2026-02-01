using RimWorld.Planet;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded;

public class Ability_BerserkPulse : Ability
{
	public IntVec3 targetCell;

	public override void ModifyTargets(ref GlobalTargetInfo[] targets)
	{
		targetCell = targets[0].Cell;
		((Ability)this).ModifyTargets(ref targets);
	}

	public override void Cast(params GlobalTargetInfo[] targets)
	{
		((Ability)this).Cast(targets);
		Ability.MakeStaticFleck(targetCell, base.pawn.Map, VPE_DefOf.PsycastAreaEffect, ((Ability)this).GetRadiusForPawn(), 0f);
	}
}
