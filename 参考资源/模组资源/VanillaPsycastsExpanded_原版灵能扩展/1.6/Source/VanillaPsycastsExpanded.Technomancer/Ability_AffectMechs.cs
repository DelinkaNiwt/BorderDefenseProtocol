using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded.Technomancer;

public class Ability_AffectMechs : Ability
{
	public override void Cast(params GlobalTargetInfo[] targets)
	{
		((Ability)this).Cast(targets);
		for (int i = 0; i < targets.Length; i++)
		{
			GlobalTargetInfo globalTargetInfo = targets[i];
			foreach (Thing item in AllTargetsAt(globalTargetInfo.Cell, globalTargetInfo.Map).InRandomOrder().Take(3))
			{
				((Ability)this).ApplyHediffs(new GlobalTargetInfo[1]
				{
					new GlobalTargetInfo(item)
				});
				item.TryGetComp<CompHaywire>()?.GoHaywire(((Ability)this).GetDurationForPawn());
			}
		}
	}

	public override void DrawHighlight(LocalTargetInfo target)
	{
		((Ability)this).DrawHighlight(target);
		foreach (Thing item in AllTargetsAt(target.Cell))
		{
			GenDraw.DrawTargetHighlight(item);
		}
	}

	private IEnumerable<Thing> AllTargetsAt(IntVec3 cell, Map map = null)
	{
		foreach (Thing thing in GenRadial.RadialDistinctThingsAround(cell, map ?? base.pawn.Map, ((Ability)this).GetRadiusForPawn(), useCenter: true))
		{
			if (thing is Building_Turret)
			{
				yield return thing;
			}
			if (thing is Pawn { RaceProps: { IsMechanoid: not false } })
			{
				yield return thing;
			}
		}
	}
}
