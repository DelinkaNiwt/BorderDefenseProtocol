using System;
using System.Linq;
using RimWorld;
using Verse;

namespace NiceInventoryTab;

internal class HediffUtility
{
	public static Predicate<Hediff> IsPermanent_or_IsImplant = delegate(Hediff x)
	{
		if (x.def == HediffDefOf.MissingBodyPart && x.Visible)
		{
			return false;
		}
		if (x.Bleeding)
		{
			return false;
		}
		if (x.def.isBad && x is HediffWithComps hd)
		{
			HediffComp_TendDuration hediffComp_TendDuration = hd.TryGetComp<HediffComp_TendDuration>();
			if (hediffComp_TendDuration != null && !hediffComp_TendDuration.IsTended)
			{
				return false;
			}
		}
		return x.IsPermanent() || x.def.countsAsAddedPartOrImplant;
	};

	public static Predicate<Hediff> Ideal = (Hediff x) => false;

	public static float CalculateCapacityLevel(Pawn pawn, PawnCapacityDef capacity, Predicate<Hediff> shouldInclude)
	{
		return PawnCapacityUtility.CalculateCapacityLevel(new HediffSet(pawn)
		{
			hediffs = pawn.health.hediffSet.hediffs.Where((Hediff x) => shouldInclude(x)).ToList()
		}, capacity);
	}

	public static (float original, float filtered) GetStatValueFilterHediffs(Pawn pawn, StatDef stat, Predicate<Hediff> includeFilter, bool applyPostProcess = true)
	{
		if (pawn.health?.hediffSet == null)
		{
			return (original: pawn.GetStatValue(stat, applyPostProcess), filtered: pawn.GetStatValue(stat, applyPostProcess));
		}
		float statValue = pawn.GetStatValue(stat, applyPostProcess);
		HediffSet hediffSet = pawn.health.hediffSet;
		try
		{
			HediffSet hediffSet2 = new HediffSet(pawn);
			foreach (Hediff hediff in hediffSet.hediffs)
			{
				if (includeFilter(hediff))
				{
					hediffSet2.hediffs.Add(hediff);
				}
			}
			pawn.health.hediffSet = hediffSet2;
			pawn.health.capacities.Clear();
			float statValue2 = pawn.GetStatValue(stat, applyPostProcess);
			return (original: statValue, filtered: statValue2);
		}
		finally
		{
			pawn.health.hediffSet = hediffSet;
			pawn.health.capacities.Clear();
		}
	}

	public static (T original, T filtered) EvaluateWithFilteredHediffs<T>(Pawn pawn, Predicate<Hediff> includeFilter, Func<Pawn, bool, T> evaluator, bool clearCapacities = true)
	{
		if (pawn.health?.hediffSet == null)
		{
			T val = evaluator(pawn, arg2: false);
			return (original: val, filtered: val);
		}
		T item = evaluator(pawn, arg2: false);
		HediffSet hediffSet = pawn.health.hediffSet;
		try
		{
			HediffSet hediffSet2 = new HediffSet(pawn);
			foreach (Hediff hediff in hediffSet.hediffs)
			{
				if (includeFilter(hediff))
				{
					hediffSet2.hediffs.Add(hediff);
				}
			}
			pawn.health.hediffSet = hediffSet2;
			if (clearCapacities)
			{
				pawn.health.capacities.Clear();
			}
			T item2 = evaluator(pawn, arg2: true);
			return (original: item, filtered: item2);
		}
		finally
		{
			pawn.health.hediffSet = hediffSet;
			if (clearCapacities)
			{
				pawn.health.capacities.Clear();
			}
		}
	}
}
