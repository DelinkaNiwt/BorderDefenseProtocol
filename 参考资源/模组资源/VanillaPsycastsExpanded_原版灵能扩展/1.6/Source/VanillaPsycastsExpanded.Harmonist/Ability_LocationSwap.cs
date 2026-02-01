using RimWorld.Planet;
using VanillaPsycastsExpanded.Skipmaster;
using VEF.Abilities;

namespace VanillaPsycastsExpanded.Harmonist;

public class Ability_LocationSwap : Ability_Teleport
{
	public override void ModifyTargets(ref GlobalTargetInfo[] targets)
	{
		targets = new GlobalTargetInfo[4]
		{
			targets[0],
			new GlobalTargetInfo(((Ability)this).pawn.Position, ((Ability)this).pawn.Map),
			((Ability)this).pawn,
			new GlobalTargetInfo(targets[0].Cell, targets[0].Map)
		};
	}
}
