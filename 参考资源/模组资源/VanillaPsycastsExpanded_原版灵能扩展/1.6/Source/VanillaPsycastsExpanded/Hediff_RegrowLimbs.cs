using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace VanillaPsycastsExpanded;

public class Hediff_RegrowLimbs : HediffWithComps
{
	public override void PostTick()
	{
		base.PostTick();
		if (Find.TickManager.TicksGame % 2500 != 0)
		{
			return;
		}
		bool flag = false;
		List<Hediff_Injury> list = pawn.health.hediffSet.hediffs.OfType<Hediff_Injury>().ToList();
		if (list.Any())
		{
			list.RandomElement().Heal(1f);
			flag = true;
		}
		else
		{
			List<BodyPartRecord> nonMissingParts = pawn.health.hediffSet.GetNotMissingParts().ToList();
			List<BodyPartRecord> list2 = pawn.def.race.body.AllParts.Where((BodyPartRecord x) => pawn.health.hediffSet.PartIsMissing(x) && nonMissingParts.Contains(x.parent) && !pawn.health.hediffSet.AncestorHasDirectlyAddedParts(x)).ToList();
			if (list2.Any())
			{
				BodyPartRecord bodyPartRecord = list2.RandomElement();
				List<Hediff_MissingPart> source = pawn.health.hediffSet.hediffs.OfType<Hediff_MissingPart>().ToList();
				pawn.health.RestorePart(bodyPartRecord);
				List<Hediff_MissingPart> currentMissingHediffs2 = pawn.health.hediffSet.hediffs.OfType<Hediff_MissingPart>().ToList();
				foreach (Hediff_MissingPart item in source.Where((Hediff_MissingPart x) => !currentMissingHediffs2.Contains(x)))
				{
					Hediff hediff = HediffMaker.MakeHediff(VPE_DefOf.VPE_Regenerating, pawn, item.Part);
					hediff.Severity = item.Part.def.GetMaxHealth(pawn) - 1f;
					pawn.health.AddHediff(hediff);
				}
				flag = true;
			}
		}
		if (flag)
		{
			FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, FleckDefOf.HealingCross);
		}
	}
}
