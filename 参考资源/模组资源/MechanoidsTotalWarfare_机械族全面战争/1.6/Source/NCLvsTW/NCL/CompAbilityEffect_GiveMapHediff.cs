using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace NCL;

public class CompAbilityEffect_GiveMapHediff : CompAbilityEffect_WithDuration
{
	public new CompProperties_AbilityGiveMapHediff Props => (CompProperties_AbilityGiveMapHediff)props;

	public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
	{
		base.Apply(target, dest);
		Pawn pawn = parent.pawn;
		Map map = pawn.Map;
		if (map == null)
		{
			return;
		}
		Faction casterFaction = pawn.Faction;
		IEnumerable<Pawn> enumerable;
		if (casterFaction != null)
		{
			enumerable = (Props.onlyPawnsInSameFaction ? map.mapPawns.SpawnedPawnsInFaction(casterFaction).Where(ValidPawn) : ((!Props.ignorePawnsInSameFaction) ? map.mapPawns.AllPawnsSpawned.Where(ValidPawn) : map.mapPawns.AllPawnsSpawned.Where((Pawn p) => p.Faction != casterFaction && ValidPawn(p))));
		}
		else
		{
			if (Props.onlyPawnsInSameFaction)
			{
				return;
			}
			enumerable = map.mapPawns.AllPawnsSpawned.Where(ValidPawn);
		}
		if (!enumerable.Any())
		{
			return;
		}
		foreach (Pawn target2 in enumerable)
		{
			ApplyInner(target2, pawn);
		}
	}

	protected void ApplyInner(Pawn target, Pawn other)
	{
		if (Props.replaceExisting)
		{
			Hediff existingHediff = target.health.hediffSet.GetFirstHediffOfDef(Props.hediffDef);
			if (existingHediff != null)
			{
				target.health.RemoveHediff(existingHediff);
			}
		}
		Hediff hediff = HediffMaker.MakeHediff(Props.hediffDef, target, Props.onlyBrain ? target.health.hediffSet.GetBrain() : null);
		HediffComp_Disappears disappearsComp = hediff.TryGetComp<HediffComp_Disappears>();
		if (disappearsComp != null)
		{
			disappearsComp.ticksToDisappear = GetDurationSeconds(target).SecondsToTicks();
		}
		if (Props.severity >= 0f)
		{
			hediff.Severity = Props.severity;
		}
		HediffComp_Link linkComp = hediff.TryGetComp<HediffComp_Link>();
		if (linkComp != null)
		{
			linkComp.other = other;
			linkComp.drawConnection = target == parent.pawn;
		}
		target.health.AddHediff(hediff);
	}

	protected bool ValidPawn(Pawn pawn)
	{
		if (pawn == null || pawn.Dead || pawn.Destroyed)
		{
			return false;
		}
		if (Props.ignoreSelf && pawn == parent.pawn)
		{
			return false;
		}
		if (!pawn.RaceProps.IsMechanoid)
		{
			return false;
		}
		if (parent.pawn.Faction != null && pawn.Faction != parent.pawn.Faction)
		{
			return false;
		}
		if (Props.inavailablePawnKinds != null && Props.inavailablePawnKinds.Contains(pawn.kindDef))
		{
			return false;
		}
		return true;
	}

	public override bool AICanTargetNow(LocalTargetInfo target)
	{
		return parent.def.aiCanUse && target.Pawn != null;
	}
}
