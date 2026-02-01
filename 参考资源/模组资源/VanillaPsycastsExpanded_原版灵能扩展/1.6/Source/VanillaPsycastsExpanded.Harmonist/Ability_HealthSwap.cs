using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded.Harmonist;

public class Ability_HealthSwap : Ability
{
	public override void Cast(params GlobalTargetInfo[] targets)
	{
		((Ability)this).Cast(targets);
		if (!(targets[0].Thing is Pawn pawn) || !(targets[1].Thing is Pawn pawn2))
		{
			return;
		}
		MoteBetween obj = (MoteBetween)ThingMaker.MakeThing(VPE_DefOf.VPE_PsycastPsychicEffectTransfer);
		obj.Attach(pawn, pawn2);
		obj.Scale = 1f;
		obj.exactPosition = pawn.DrawPos;
		GenSpawn.Spawn(obj, pawn.Position, pawn.MapHeld);
		List<Hediff> list = pawn.health.hediffSet.hediffs.Where(ShouldTransfer).ToList();
		List<Hediff> list2 = pawn2.health.hediffSet.hediffs.Where(ShouldTransfer).ToList();
		foreach (Hediff item in list)
		{
			pawn.health.RemoveHediff(item);
		}
		foreach (Hediff item2 in list2)
		{
			pawn2.health.RemoveHediff(item2);
		}
		AddAll(pawn, list2);
		AddAll(pawn2, list);
	}

	private static bool ShouldTransfer(Hediff hediff)
	{
		bool flag = ((hediff is Hediff_Injury || hediff is Hediff_MissingPart || hediff is Hediff_Addiction) ? true : false);
		if (!flag && !hediff.def.tendable && !hediff.def.makesSickThought)
		{
			return hediff.def.HasComp(typeof(HediffComp_Immunizable));
		}
		return true;
	}

	private static void AddAll(Pawn pawn, List<Hediff> hediffs)
	{
		TryAdd();
		TryAdd();
		void TryAdd()
		{
			hediffs.RemoveAll(delegate(Hediff hediff)
			{
				if (pawn.health.hediffSet.PartIsMissing(hediff.Part))
				{
					return false;
				}
				try
				{
					pawn.health.AddHediff(hediff, hediff.Part);
					return true;
				}
				catch (Exception arg)
				{
					Log.Error($"Error while swapping: {arg}");
					return false;
				}
			});
		}
	}
}
