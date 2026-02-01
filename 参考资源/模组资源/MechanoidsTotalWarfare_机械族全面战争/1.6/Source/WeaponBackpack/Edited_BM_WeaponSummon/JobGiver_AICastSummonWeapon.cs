using RimWorld;
using Verse;

namespace Edited_BM_WeaponSummon;

public class JobGiver_AICastSummonWeapon : JobGiver_AICastAbility
{
	protected override LocalTargetInfo GetTarget(Pawn caster, Ability ability)
	{
		ThingWithComps thingWithComps = caster.equipment?.Primary;
		if (thingWithComps != null && thingWithComps.GetComp<CompSummonedWeapon>() == null)
		{
			return caster;
		}
		return LocalTargetInfo.Invalid;
	}
}
