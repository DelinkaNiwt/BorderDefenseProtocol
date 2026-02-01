using System.Linq;
using RimWorld;
using RimWorld.Planet;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded;

public class AbilityExtension_GiveMentalState : AbilityExtension_AbilityMod
{
	public bool applyToSelf;

	public bool clearOthers;

	public StatDef durationMultiplier;

	public bool durationScalesWithCaster;

	public MentalStateDef stateDef;

	public MentalStateDef stateDefForMechs;

	public override void Cast(GlobalTargetInfo[] targets, Ability ability)
	{
		((AbilityExtension_AbilityMod)this).Cast(targets, ability);
		foreach (GlobalTargetInfo globalTargetInfo in targets)
		{
			Pawn pawn = (applyToSelf ? ability.pawn : (globalTargetInfo.Thing as Pawn));
			if (pawn == null)
			{
				continue;
			}
			if (pawn.InMentalState)
			{
				if (!clearOthers)
				{
					continue;
				}
				pawn.mindState.mentalStateHandler.CurState.RecoverFromState();
			}
			TryGiveMentalStateWithDuration(pawn.RaceProps.IsMechanoid ? (stateDefForMechs ?? stateDef) : stateDef, pawn, ability, durationMultiplier, durationScalesWithCaster);
			RestUtility.WakeUp(pawn);
		}
	}

	public override bool Valid(GlobalTargetInfo[] targets, Ability ability, bool throwMessages = false)
	{
		Pawn pawn = targets.Select((GlobalTargetInfo t) => t.Thing).OfType<Pawn>().FirstOrDefault();
		if (pawn != null && !AbilityUtility.ValidateNoMentalState(pawn, throwMessages, null))
		{
			return false;
		}
		return true;
	}

	public static void TryGiveMentalStateWithDuration(MentalStateDef def, Pawn p, Ability ability, StatDef multiplierStat, bool durationScalesWithCaster)
	{
		if (p.mindState.mentalStateHandler.TryStartMentalState(def, null, forced: true, forceWake: false, causedByMood: false, null, transitionSilently: false, causedByDamage: false, ((Def)(object)ability.def).GetModExtension<AbilityExtension_Psycast>() != null))
		{
			float num = ability.GetDurationForPawn();
			if (multiplierStat != null)
			{
				num = ((!durationScalesWithCaster) ? (num * ability.pawn.GetStatValue(multiplierStat)) : (num * p.GetStatValue(multiplierStat)));
			}
			p.mindState.mentalStateHandler.CurState.forceRecoverAfterTicks = (int)num;
		}
	}
}
