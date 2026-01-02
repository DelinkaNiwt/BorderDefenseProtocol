using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Milira;

public class CompAbilityEffect_MechRecover : CompAbilityEffect
{
	private new CompProperties_MechRecover Props => (CompProperties_MechRecover)props;

	public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
	{
		base.Apply(target, dest);
		Pawn pawn = target.Pawn;
		if (pawn == null)
		{
			return;
		}
		int num = 0;
		List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
		List<Hediff_Injury> list = new List<Hediff_Injury>();
		foreach (Hediff item2 in hediffs)
		{
			if (item2 is Hediff_Injury item)
			{
				list.Add(item);
			}
		}
		foreach (Hediff_Injury item3 in list)
		{
			pawn.health.RemoveHediff(item3);
			num++;
		}
		parent.StartCooldown(900 * num);
		if (num > 0)
		{
			MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "NumWoundsTended".Translate(num), 3.65f);
		}
		FleckMaker.AttachedOverlay(pawn, FleckDefOf.FlashHollow, Vector3.zero, 1.5f);
	}

	public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
	{
		Pawn pawn = target.Pawn;
		if (pawn != null)
		{
			List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
			for (int i = 0; i < hediffs.Count; i++)
			{
				if (hediffs[i] is Hediff_Injury || hediffs[i] is Hediff_MissingPart)
				{
					return true;
				}
			}
		}
		return base.Valid(target, throwMessages);
	}
}
