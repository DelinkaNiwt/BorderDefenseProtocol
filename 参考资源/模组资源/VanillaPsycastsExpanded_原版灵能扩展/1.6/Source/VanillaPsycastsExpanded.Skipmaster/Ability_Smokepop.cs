using RimWorld;
using RimWorld.Planet;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded.Skipmaster;

public class Ability_Smokepop : Ability
{
	public override void Cast(params GlobalTargetInfo[] targets)
	{
		((Ability)this).Cast(targets);
		for (int i = 0; i < targets.Length; i++)
		{
			GlobalTargetInfo globalTargetInfo = targets[i];
			GenExplosion.DoExplosion(globalTargetInfo.Cell, globalTargetInfo.Map, ((Ability)this).GetRadiusForPawn(), DamageDefOf.Smoke, base.pawn, -1, -1f, null, null, null, null, null, 0f, 1, GasType.BlindSmoke);
		}
	}
}
