using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded;

public class AbilityExtension_AbilityOffsetPrisonerResistance : AbilityExtension_AbilityMod
{
	public float offset;

	public override void Cast(GlobalTargetInfo[] targets, Ability ability)
	{
		((AbilityExtension_AbilityMod)this).Cast(targets, ability);
		foreach (GlobalTargetInfo globalTargetInfo in targets)
		{
			if (globalTargetInfo.Thing is Pawn pawn)
			{
				float num = offset * pawn.GetStatValue(StatDefOf.PsychicSensitivity);
				pawn.guest.resistance = Mathf.Max(pawn.guest.resistance + num, 0f);
			}
		}
	}

	public override bool CanApplyOn(LocalTargetInfo target, Ability ability, bool throwMessages = false)
	{
		Pawn pawn = target.Pawn;
		if (pawn != null)
		{
			if (!pawn.IsPrisonerOfColony)
			{
				return false;
			}
			if (pawn != null && pawn.guest.resistance < float.Epsilon)
			{
				return false;
			}
			if (pawn.Downed)
			{
				return false;
			}
			return ((AbilityExtension_AbilityMod)this).Valid(new GlobalTargetInfo[1] { target.ToGlobalTargetInfo(target.Thing.Map) }, ability, false);
		}
		return false;
	}

	public override bool Valid(GlobalTargetInfo[] targets, Ability ability, bool throwMessages = false)
	{
		foreach (GlobalTargetInfo globalTargetInfo in targets)
		{
			if (globalTargetInfo.Thing is Pawn targetPawn && !AbilityUtility.ValidateHasResistance(targetPawn, throwMessages, null))
			{
				return false;
			}
		}
		return ((AbilityExtension_AbilityMod)this).Valid(targets, ability, throwMessages);
	}
}
