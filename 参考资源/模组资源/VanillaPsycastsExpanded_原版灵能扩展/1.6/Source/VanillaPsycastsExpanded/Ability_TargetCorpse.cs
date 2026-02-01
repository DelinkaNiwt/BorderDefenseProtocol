using RimWorld;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded;

public abstract class Ability_TargetCorpse : Ability
{
	public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
	{
		Corpse corpse = target.Thing as Corpse;
		if (corpse == null)
		{
			if (showMessages)
			{
				Messages.Message("VPE.MustBeCorpse".Translate(), corpse, MessageTypeDefOf.CautionInput);
			}
			return false;
		}
		if (!corpse.InnerPawn.RaceProps.Humanlike)
		{
			if (showMessages)
			{
				Messages.Message("VPE.MustBeCorpseHumanlike".Translate(), corpse, MessageTypeDefOf.CautionInput);
			}
			return false;
		}
		return ((Ability)this).ValidateTarget(target, showMessages);
	}
}
