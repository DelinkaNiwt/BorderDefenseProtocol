using RimWorld;
using RimWorld.Planet;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded.Technomancer;

public class Ability_Power : Ability
{
	public override void Cast(params GlobalTargetInfo[] targets)
	{
		((Ability)this).Cast(targets);
		foreach (GlobalTargetInfo globalTargetInfo in targets)
		{
			((Ability)this).ApplyHediff(base.pawn)?.TryGetComp<HediffComp_InfinitePower>().Begin(globalTargetInfo.Thing);
		}
	}

	public override Hediff ApplyHediff(Pawn targetPawn, HediffDef hediffDef, BodyPartRecord bodyPart, int duration, float severity)
	{
		Hediff hediff = HediffMaker.MakeHediff(hediffDef, targetPawn, bodyPart);
		Hediff_Ability val = (Hediff_Ability)(object)((hediff is Hediff_Ability) ? hediff : null);
		if (val != null)
		{
			val.ability = (Ability)(object)this;
		}
		if (severity > float.Epsilon)
		{
			hediff.Severity = severity;
		}
		if (hediff is HediffWithComps hediffWithComps)
		{
			foreach (HediffComp comp in hediffWithComps.comps)
			{
				HediffComp_Ability val2 = (HediffComp_Ability)(object)((comp is HediffComp_Ability) ? comp : null);
				if (val2 != null)
				{
					val2.ability = (Ability)(object)this;
				}
				if (comp is HediffComp_Disappears hediffComp_Disappears)
				{
					hediffComp_Disappears.ticksToDisappear = duration;
				}
			}
		}
		targetPawn.health.AddHediff(hediff);
		return hediff;
	}

	public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
	{
		if (!((Ability)this).ValidateTarget(target, showMessages))
		{
			return false;
		}
		CompPowerTrader compPowerTrader = target.Thing?.TryGetComp<CompPowerTrader>();
		if (compPowerTrader != null && compPowerTrader.PowerOutput < 0f)
		{
			return true;
		}
		if (ModsConfig.BiotechActive && target.Thing is Pawn { RaceProps: { IsMechanoid: not false }, needs: { energy: not null } } pawn && pawn.IsMechAlly(base.pawn))
		{
			return true;
		}
		if (showMessages)
		{
			Messages.Message("VPE.MustConsumePower".Translate(), MessageTypeDefOf.RejectInput, historical: false);
		}
		return false;
	}
}
