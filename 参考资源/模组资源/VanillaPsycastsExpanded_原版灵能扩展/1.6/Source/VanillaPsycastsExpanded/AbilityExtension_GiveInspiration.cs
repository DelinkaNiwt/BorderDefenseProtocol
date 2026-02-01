using RimWorld;
using RimWorld.Planet;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded;

public class AbilityExtension_GiveInspiration : AbilityExtension_AbilityMod
{
	public bool onlyPlayer;

	public override void Cast(GlobalTargetInfo[] targets, Ability ability)
	{
		((AbilityExtension_AbilityMod)this).Cast(targets, ability);
		foreach (GlobalTargetInfo globalTargetInfo in targets)
		{
			if (!(globalTargetInfo.Thing is Pawn pawn))
			{
				continue;
			}
			if (onlyPlayer)
			{
				Faction faction = pawn.Faction;
				if (faction == null || !faction.IsPlayer)
				{
					continue;
				}
			}
			InspirationDef randomAvailableInspirationDef = pawn.mindState.inspirationHandler.GetRandomAvailableInspirationDef();
			if (randomAvailableInspirationDef != null)
			{
				pawn.mindState.inspirationHandler.TryStartInspiration(randomAvailableInspirationDef, "LetterPsychicInspiration".Translate(pawn.Named("PAWN"), ability.pawn.Named("CASTER")));
			}
		}
	}

	public override bool CanApplyOn(LocalTargetInfo target, Ability ability, bool throwMessages = false)
	{
		return ((AbilityExtension_AbilityMod)this).Valid(new GlobalTargetInfo[1] { target.ToGlobalTargetInfo(target.Thing.Map) }, ability, false);
	}

	public override bool Valid(GlobalTargetInfo[] targets, Ability ability, bool throwMessages = false)
	{
		foreach (GlobalTargetInfo globalTargetInfo in targets)
		{
			if (!(globalTargetInfo.Thing is Pawn pawn))
			{
				continue;
			}
			if (onlyPlayer)
			{
				Faction faction = pawn.Faction;
				if (faction == null || !faction.IsPlayer)
				{
					continue;
				}
			}
			if (!AbilityUtility.ValidateNoInspiration(pawn, throwMessages, null))
			{
				return false;
			}
			if (!AbilityUtility.ValidateCanGetInspiration(pawn, throwMessages, null))
			{
				return false;
			}
		}
		return ((AbilityExtension_AbilityMod)this).Valid(targets, ability, throwMessages);
	}
}
