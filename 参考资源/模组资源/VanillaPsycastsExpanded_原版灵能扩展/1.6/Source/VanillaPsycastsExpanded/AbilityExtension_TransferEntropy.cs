using RimWorld;
using RimWorld.Planet;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded;

public class AbilityExtension_TransferEntropy : AbilityExtension_AbilityMod
{
	public bool targetReceivesEntropy = true;

	public override void Cast(GlobalTargetInfo[] targets, Ability ability)
	{
		((AbilityExtension_AbilityMod)this).Cast(targets, ability);
		foreach (GlobalTargetInfo globalTargetInfo in targets)
		{
			if (globalTargetInfo.Thing is Pawn pawn)
			{
				if (targetReceivesEntropy)
				{
					pawn.psychicEntropy.TryAddEntropy(ability.pawn.psychicEntropy.EntropyValue, ability.pawn, scale: false, overLimit: true);
				}
				if (!pawn.HasPsylink)
				{
					Hediff hediff = HediffMaker.MakeHediff(VPE_DefOf.PsychicComa, pawn);
					pawn.health.AddHediff(hediff);
				}
				ability.pawn.psychicEntropy.RemoveAllEntropy();
				MoteMaker.MakeInteractionOverlay(ThingDefOf.Mote_PsychicLinkPulse, ability.pawn, pawn);
			}
		}
	}

	public override bool IsEnabledForPawn(Ability ability, out string reason)
	{
		if (ability.pawn.psychicEntropy.EntropyValue <= 0f)
		{
			reason = "AbilityNoEntropyToDump".Translate();
			return false;
		}
		return ((AbilityExtension_AbilityMod)this).IsEnabledForPawn(ability, ref reason);
	}

	public override bool Valid(GlobalTargetInfo[] targets, Ability ability, bool throwMessages = false)
	{
		foreach (GlobalTargetInfo globalTargetInfo in targets)
		{
			if (globalTargetInfo.Thing is Pawn targetPawn && !AbilityUtility.ValidateNoMentalState(targetPawn, throwMessages, null))
			{
				return false;
			}
		}
		return ((AbilityExtension_AbilityMod)this).Valid(targets, ability, throwMessages);
	}
}
