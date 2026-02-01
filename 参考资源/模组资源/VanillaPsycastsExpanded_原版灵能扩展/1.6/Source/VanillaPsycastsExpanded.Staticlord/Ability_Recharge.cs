using RimWorld;
using RimWorld.Planet;
using VanillaPsycastsExpanded.Technomancer;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded.Staticlord;

public class Ability_Recharge : Ability
{
	public override void Cast(params GlobalTargetInfo[] targets)
	{
		((Ability)this).Cast(targets);
		foreach (GlobalTargetInfo globalTargetInfo in targets)
		{
			Hediff hediff = HediffMaker.MakeHediff(VPE_DefOf.VPE_Recharge, base.pawn);
			hediff.TryGetComp<HediffComp_Recharge>().Init(globalTargetInfo.Thing);
			hediff.TryGetComp<HediffComp_Disappears>().ticksToDisappear = ((Ability)this).GetDurationForPawn();
			base.pawn.health.AddHediff(hediff);
		}
	}

	public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
	{
		if (!((Ability)this).ValidateTarget(target, showMessages))
		{
			return false;
		}
		if (target.Thing?.TryGetComp<CompPowerBattery>() != null)
		{
			return true;
		}
		if (ModsConfig.BiotechActive && target.Thing is Pawn { RaceProps: { IsMechanoid: not false }, needs: { energy: not null } } pawn && pawn.IsMechAlly(base.pawn))
		{
			return true;
		}
		if (showMessages)
		{
			Messages.Message("VPE.MustTargetBattery".Translate(), MessageTypeDefOf.RejectInput, historical: false);
		}
		return false;
	}
}
