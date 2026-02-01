using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded;

public class AbilityExtension_WordOfLove : AbilityExtension_AbilityMod
{
	public override bool HidePawnTooltips => true;

	public override void Cast(GlobalTargetInfo[] targets, Ability ability)
	{
		((AbilityExtension_AbilityMod)this).Cast(targets, ability);
		Pawn target = targets[1].Thing as Pawn;
		Pawn pawn = targets[0].Thing as Pawn;
		Hediff firstHediffOfDef = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PsychicLove);
		if (firstHediffOfDef != null)
		{
			pawn.health.RemoveHediff(firstHediffOfDef);
		}
		Hediff_PsychicLove hediff_PsychicLove = (Hediff_PsychicLove)HediffMaker.MakeHediff(HediffDefOf.PsychicLove, pawn, pawn.health.hediffSet.GetBrain());
		hediff_PsychicLove.target = target;
		HediffComp_Disappears hediffComp_Disappears = hediff_PsychicLove.TryGetComp<HediffComp_Disappears>();
		if (hediffComp_Disappears != null)
		{
			hediffComp_Disappears.ticksToDisappear = (int)((float)ability.GetDurationForPawn() * pawn.GetStatValue(StatDefOf.PsychicSensitivity));
		}
		pawn.health.AddHediff(hediff_PsychicLove);
	}

	public override string ExtraLabelMouseAttachment(LocalTargetInfo target, Ability ability)
	{
		if (ability.currentTargets.Where((GlobalTargetInfo x) => x.Thing != null).ToList().Any())
		{
			return "PsychicLoveFor".Translate();
		}
		return "PsychicLoveInduceIn".Translate();
	}

	public override bool ValidateTarget(LocalTargetInfo target, Ability ability, bool showMessages = true)
	{
		List<GlobalTargetInfo> list = ability.currentTargets.Where((GlobalTargetInfo x) => x.Thing != null).ToList();
		if (list.Any())
		{
			Pawn pawn = list[0].Thing as Pawn;
			Pawn pawn2 = target.Pawn;
			if (pawn == pawn2)
			{
				return false;
			}
			if (pawn != null && pawn2 != null && !pawn.story.traits.HasTrait(TraitDefOf.Bisexual))
			{
				Gender gender = pawn.gender;
				Gender gender2 = (pawn.story.traits.HasTrait(TraitDefOf.Gay) ? gender : gender.Opposite());
				if (pawn2.gender != gender2)
				{
					if (showMessages)
					{
						Messages.Message("AbilityCantApplyWrongAttractionGender".Translate(pawn, pawn2), pawn, MessageTypeDefOf.RejectInput, historical: false);
					}
					return false;
				}
			}
			return true;
		}
		return ((AbilityExtension_AbilityMod)this).ValidateTarget(target, ability, showMessages);
	}

	public override bool Valid(GlobalTargetInfo[] targets, Ability ability, bool throwMessages = false)
	{
		foreach (GlobalTargetInfo globalTargetInfo in targets)
		{
			if (!(globalTargetInfo.Thing is Pawn pawn))
			{
				continue;
			}
			if (pawn.story.traits.HasTrait(TraitDefOf.Asexual))
			{
				if (throwMessages)
				{
					Messages.Message("AbilityCantApplyOnAsexual".Translate(((Def)(object)ability.def).label), pawn, MessageTypeDefOf.RejectInput, historical: false);
				}
				return false;
			}
			if (!AbilityUtility.ValidateNoMentalState(pawn, throwMessages, null))
			{
				return false;
			}
		}
		return ((AbilityExtension_AbilityMod)this).Valid(targets, ability, throwMessages);
	}
}
