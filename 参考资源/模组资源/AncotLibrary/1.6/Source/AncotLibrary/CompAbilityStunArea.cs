using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompAbilityStunArea : CompAbilityEffect
{
	public new CompProperties_AbilityStunArea Props => (CompProperties_AbilityStunArea)props;

	public virtual float RadiusBase => Props.radius;

	public Pawn Caster => parent.pawn;

	public override bool AICanTargetNow(LocalTargetInfo target)
	{
		return true;
	}

	public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
	{
		if (Props.targetOnCaster)
		{
			target = Caster;
		}
		List<Thing> list = new List<Thing>();
		list = GenRadial.RadialDistinctThingsAround(target.Cell, Caster.Map, RadiusBase, useCenter: true).ToList();
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i] is Pawn pawn && IsPawnAffected(pawn))
			{
				pawn.stances.stunner.StunFor(Props.stunForTicks, null);
			}
		}
		if (Props.effecter != null)
		{
			Effecter effecter = new Effecter(Props.effecter);
			effecter.Trigger(new TargetInfo(target.Cell, Caster.Map), TargetInfo.Invalid);
			effecter.Cleanup();
		}
	}

	public virtual bool IsPawnAffected(Pawn pawn)
	{
		if (Props.applyOnAllyOnly && pawn.Faction != Caster.Faction)
		{
			return false;
		}
		if (!Props.applyOnAlly && pawn.Faction == Caster.Faction)
		{
			return false;
		}
		if (!Props.applyOnMech && pawn.RaceProps.IsMechanoid)
		{
			return false;
		}
		if (Props.ignoreCaster && pawn == Caster)
		{
			return false;
		}
		return true;
	}
}
