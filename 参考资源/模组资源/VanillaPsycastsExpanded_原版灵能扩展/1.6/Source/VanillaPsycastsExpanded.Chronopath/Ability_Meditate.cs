using RimWorld.Planet;
using VEF.Abilities;

namespace VanillaPsycastsExpanded.Chronopath;

public class Ability_Meditate : Ability
{
	public override void Cast(params GlobalTargetInfo[] targets)
	{
		((Ability)this).Cast(targets);
		base.pawn.psychicEntropy.OffsetPsyfocusDirectly(1f - base.pawn.psychicEntropy.CurrentPsyfocus);
		base.pawn.Psycasts().GainExperience(300f);
	}
}
