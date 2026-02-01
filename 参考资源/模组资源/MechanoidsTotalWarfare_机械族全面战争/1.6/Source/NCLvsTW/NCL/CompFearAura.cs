using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace NCL;

public class CompFearAura : ThingComp
{
	private int nextCheckTick = 0;

	private CompProperties_FearAura Props => (CompProperties_FearAura)props;

	public override void CompTick()
	{
		base.CompTick();
		if (Find.TickManager.TicksGame < nextCheckTick || !parent.Spawned || parent.Map == null)
		{
			return;
		}
		nextCheckTick = Find.TickManager.TicksGame + Props.checkInterval;
		foreach (Pawn pawn in GetNearbyPawns())
		{
			if (ShouldFlee(pawn) && Rand.Value < Props.fleeChance)
			{
				MakePawnFlee(pawn);
			}
		}
	}

	private IEnumerable<Pawn> GetNearbyPawns()
	{
		return from p in GenRadial.RadialDistinctThingsAround(parent.Position, parent.Map, Props.radius, useCenter: true).OfType<Pawn>()
			where p != parent && !p.Dead && !p.Downed && p.Awake()
			select p;
	}

	private bool ShouldFlee(Pawn pawn)
	{
		if (!AffectsPawnBasedOnFaction(pawn))
		{
			return false;
		}
		if (pawn.RaceProps.Humanlike)
		{
			return Props.affectHumans;
		}
		if (pawn.RaceProps.Animal)
		{
			return Props.affectAnimals;
		}
		if (pawn.RaceProps.IsMechanoid)
		{
			return Props.affectMechanoids;
		}
		return false;
	}

	private bool AffectsPawnBasedOnFaction(Pawn pawn)
	{
		Faction parentFaction = parent.Faction;
		Faction pawnFaction = pawn.Faction;
		if (parentFaction == null || pawnFaction == null)
		{
			return true;
		}
		if (parentFaction == pawnFaction)
		{
			return false;
		}
		if (parentFaction.def == pawnFaction.def && parentFaction.loadID == pawnFaction.loadID)
		{
			return false;
		}
		if (!Props.affectAllies)
		{
			try
			{
				return parentFaction.HostileTo(pawnFaction);
			}
			catch
			{
				return true;
			}
		}
		return true;
	}

	private void MakePawnFlee(Pawn pawn)
	{
		if (pawn.CurJob != null && pawn.CurJob.def == JobDefOf.Flee)
		{
			return;
		}
		IntVec3 direction = pawn.Position - parent.Position;
		float length = direction.LengthHorizontal;
		if (length > 0f)
		{
			direction.x = Mathf.RoundToInt((float)direction.x / length);
			direction.z = Mathf.RoundToInt((float)direction.z / length);
		}
		else
		{
			direction = new IntVec3(Rand.Range(-1, 2), 0, Rand.Range(-1, 2));
		}
		IntVec3 fleeDest = pawn.Position + direction * 10;
		fleeDest = CellFinder.RandomClosewalkCellNear(fleeDest, pawn.Map, 5);
		if (fleeDest.IsValid && pawn.CanReach(fleeDest, PathEndMode.OnCell, Danger.Deadly))
		{
			pawn.jobs.StartJob(JobMaker.MakeJob(JobDefOf.Flee, fleeDest, parent), JobCondition.InterruptForced);
			if (pawn.IsColonist)
			{
				Messages.Message(pawn.LabelShort + " is fleeing in terror!", pawn, MessageTypeDefOf.NegativeEvent);
			}
			FleckMaker.ThrowAirPuffUp(pawn.DrawPos, pawn.Map);
		}
	}

	public override string CompInspectStringExtra()
	{
		if (!parent.Spawned)
		{
			return null;
		}
		string baseText = $"Fear radius: {Props.radius}m";
		if (!Props.affectAllies)
		{
			baseText += "\nAllies are immune";
		}
		return baseText;
	}
}
