using System.Collections.Generic;
using UnityEngine;
using VEF.AnimalBehaviours;
using Verse;

namespace VanillaPsycastsExpanded;

public class CompBreakLink : ThingComp, PawnGizmoProvider
{
	public Pawn Pawn;

	public CompProperties_BreakLink Props => props as CompProperties_BreakLink;

	public IEnumerable<Gizmo> GetGizmos()
	{
		yield return new Command_Action
		{
			defaultLabel = Props.gizmoLabel.Translate(),
			defaultDesc = Props.gizmoDesc.Translate(),
			icon = ContentFinder<Texture2D>.Get(Props.gizmoImage),
			action = delegate
			{
				parent.Kill();
			}
		};
	}

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		base.PostSpawnSetup(respawningAfterLoad);
		if (parent is IMinHeatGiver giver)
		{
			Pawn.Psycasts().AddMinHeatGiver(giver);
		}
	}

	public override void CompTick()
	{
		base.CompTick();
		Pawn pawn = Pawn;
		if ((pawn == null || pawn.Dead || pawn.Destroyed) ? true : false)
		{
			parent.Kill();
		}
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_References.Look(ref Pawn, "pawn");
	}
}
