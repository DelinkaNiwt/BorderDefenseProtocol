using RimWorld;
using RimWorld.Planet;
using VEF.Abilities;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace VanillaPsycastsExpanded;

public class AbilityExtension_Flee : AbilityExtension_AbilityMod
{
	public bool onlyHostile = true;

	public override void Cast(GlobalTargetInfo[] targets, Ability ability)
	{
		((AbilityExtension_AbilityMod)this).Cast(targets, ability);
		foreach (GlobalTargetInfo globalTargetInfo in targets)
		{
			Pawn pawn = globalTargetInfo.Thing as Pawn;
			if (!onlyHostile || !pawn.HostileTo(ability.pawn))
			{
				break;
			}
			pawn.GetLord()?.RemovePawn(pawn);
			pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
			pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.PanicFlee, ((Def)(object)ability.def).label, forced: true, forceWake: false, causedByMood: false, null, transitionSilently: true, causedByDamage: false, causedByPsycast: true);
		}
	}
}
