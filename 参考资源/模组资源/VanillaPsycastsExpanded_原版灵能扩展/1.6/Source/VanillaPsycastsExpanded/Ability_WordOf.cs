using System.Collections.Generic;
using System.Linq;
using RimWorld.Planet;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded;

public class Ability_WordOf : Ability
{
	public override void Cast(params GlobalTargetInfo[] targets)
	{
		if (base.pawn.health.hediffSet.GetFirstHediffOfDef(VPE_DefOf.VPE_GroupLink) is Hediff_GroupLink hediff_GroupLink)
		{
			List<GlobalTargetInfo> list = targets.ToList();
			foreach (Pawn linkedPawn in hediff_GroupLink.linkedPawns)
			{
				if (!Enumerable.Any(list, (GlobalTargetInfo x) => x.Thing == linkedPawn))
				{
					list.Add(linkedPawn);
				}
			}
			((Ability)this).Cast(list.ToArray());
		}
		else
		{
			((Ability)this).Cast(targets);
		}
	}
}
