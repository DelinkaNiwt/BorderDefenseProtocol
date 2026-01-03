using RimWorld;
using Verse;

namespace AncotLibrary;

public class JobGiver_AICastAbilityCaregoryOnSelf : JobGiver_AICastAbilityCategory
{
	protected override LocalTargetInfo GetTarget(Pawn caster, Ability ability)
	{
		LocalTargetInfo localTargetInfo = new LocalTargetInfo(caster);
		if (!ability.def.targetRequired || ability.CanApplyOn(localTargetInfo))
		{
			return localTargetInfo;
		}
		return LocalTargetInfo.Invalid;
	}
}
