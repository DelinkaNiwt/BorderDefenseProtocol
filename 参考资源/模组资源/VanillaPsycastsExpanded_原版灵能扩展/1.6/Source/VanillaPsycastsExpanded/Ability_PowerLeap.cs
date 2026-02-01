using RimWorld;
using RimWorld.Planet;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded;

public class Ability_PowerLeap : Ability
{
	public override void Cast(params GlobalTargetInfo[] targets)
	{
		Map map = ((Ability)this).Caster.Map;
		JumpingPawn obj = (JumpingPawn)(object)PawnFlyer.MakeFlyer(VPE_DefOf.VPE_JumpingPawn, ((Ability)this).CasterPawn, targets[0].Cell, null, null);
		((AbilityPawnFlyer)obj).ability = (Ability)(object)this;
		GenSpawn.Spawn((Thing)(object)obj, ((Ability)this).Caster.Position, map);
		((Ability)this).Cast(targets);
	}
}
