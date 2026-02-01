using System.Linq;
using RimWorld;
using Verse;

namespace NCL;

public class LTS_CompAbilityEffect_ToggleHediff : CompAbilityEffect
{
	public new LTS_CompProperties_ToggleHediff Props => (LTS_CompProperties_ToggleHediff)props;

	public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
	{
		base.Apply(target, dest);
		if (target.Pawn == null)
		{
			return;
		}
		object obj = target.Pawn.health?.hediffSet?.GetFirstHediffOfDef(Props.ToggleHediff);
		if (obj != null)
		{
			target.Pawn.health.RemoveHediff(target.Pawn.health.hediffSet.GetFirstHediffOfDef(Props.ToggleHediff));
			return;
		}
		target.Pawn.health.AddHediff(Props.ToggleHediff, location(target));
		if (target.Pawn.health.hediffSet.GetFirstHediffOfDef(Props.ToggleHediff).TryGetComp<HediffComp_Lactating>() != null)
		{
			target.Pawn.health.hediffSet.GetFirstHediffOfDef(Props.ToggleHediff).TryGetComp<HediffComp_Lactating>().TryCharge(-0.124f);
		}
	}

	public BodyPartRecord location(LocalTargetInfo target)
	{
		if (Props.location == null)
		{
			return null;
		}
		return (from part in target.Pawn.health.hediffSet.GetNotMissingParts()
			where part.def == Props.location
			select part).ToList()[0];
	}
}
