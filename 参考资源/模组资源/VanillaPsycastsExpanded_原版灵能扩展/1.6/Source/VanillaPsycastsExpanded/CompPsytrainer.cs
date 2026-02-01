using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded;

public class CompPsytrainer : CompUseEffect_GiveAbility
{
	public override void DoEffect(Pawn usedBy)
	{
		PsycasterPathDef psycasterPathDef = ((CompUseEffect_GiveAbility)this).Props.ability?.Psycast()?.path;
		if (psycasterPathDef != null)
		{
			Hediff_PsycastAbilities hediff_PsycastAbilities = usedBy.Psycasts();
			if (hediff_PsycastAbilities != null && !hediff_PsycastAbilities.unlockedPaths.Contains(psycasterPathDef))
			{
				hediff_PsycastAbilities.UnlockPath(psycasterPathDef);
			}
		}
		((CompUseEffect_GiveAbility)this).DoEffect(usedBy);
	}

	public override AcceptanceReport CanBeUsedBy(Pawn p)
	{
		Hediff_PsycastAbilities hediff_PsycastAbilities = p.Psycasts();
		if ((hediff_PsycastAbilities == null || ((Hediff_Level)(object)hediff_PsycastAbilities).level <= 0) ? true : false)
		{
			return "VPE.MustBePsycaster".Translate();
		}
		PsycasterPathDef psycasterPathDef = ((CompUseEffect_GiveAbility)this).Props.ability?.Psycast()?.path;
		if (psycasterPathDef != null && !psycasterPathDef.CanPawnUnlock(p) && !psycasterPathDef.ignoreLockRestrictionsForNeurotrainers)
		{
			return ((CompUseEffect_GiveAbility)this).Props.ability.Psycast().path.lockedReason;
		}
		if (((ThingWithComps)p).GetComp<CompAbilities>().HasAbility(((CompUseEffect_GiveAbility)this).Props.ability))
		{
			return "VPE.AlreadyHasPsycast".Translate(((Def)(object)((CompUseEffect_GiveAbility)this).Props.ability).LabelCap);
		}
		return true;
	}
}
