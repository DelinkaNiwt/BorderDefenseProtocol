using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public class JobGiver_AICastAbilityOnAllyPawn : JobGiver_AICastAbility
{
	private float targetHealthPctUnder = 1f;

	private List<ThingDef> neverTargetThings = new List<ThingDef>();

	private static List<Thing> potentialTargets = new List<Thing>();

	protected override LocalTargetInfo GetTarget(Pawn caster, Ability ability)
	{
		potentialTargets.Clear();
		potentialTargets.AddRange((from t in GenRadial.RadialDistinctThingsAround(caster.Position, caster.Map, ability.verb.EffectiveRange, useCenter: true)
			where t is Pawn pawn && caster.CanReserve(t) && pawn.health.summaryHealth.SummaryHealthPercent < targetHealthPctUnder && !neverTargetThings.Contains(pawn.def) && t.Faction == caster.Faction && ability.CanApplyOn(new LocalTargetInfo(pawn))
			select t).ToList());
		if (potentialTargets.TryRandomElement(out var result))
		{
			return new LocalTargetInfo(result);
		}
		return LocalTargetInfo.Invalid;
	}
}
