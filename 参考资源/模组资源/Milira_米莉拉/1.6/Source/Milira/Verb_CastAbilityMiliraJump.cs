using RimWorld;
using Verse;

namespace Milira;

public class Verb_CastAbilityMiliraJump : Verb_CastAbilityJump
{
	public override ThingDef JumpFlyerDef => MiliraDefOf.Milira_PawnJumper;
}
