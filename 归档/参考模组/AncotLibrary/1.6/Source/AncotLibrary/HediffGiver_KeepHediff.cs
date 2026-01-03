using System.Collections.Generic;
using System.Linq;
using Verse;

namespace AncotLibrary;

public class HediffGiver_KeepHediff : HediffGiver
{
	public float severityAmount = float.NaN;

	public float mtbHours = -1f;

	private static int mtbCheckInterval = 1200;

	private static List<Hediff> addedHediffs = new List<Hediff>();

	public override void OnIntervalPassed(Pawn pawn, Hediff cause)
	{
		if (!pawn.IsNestedHashIntervalTick(60, mtbCheckInterval) || !Rand.MTBEventOccurs(mtbHours, 2500f, mtbCheckInterval))
		{
			return;
		}
		IEnumerable<BodyPartRecord> bodyParts = pawn.health.hediffSet.GetNotMissingParts();
		if (pawn.health.hediffSet.GetFirstHediffOfDef(hediff) != null || !partsToAffect.Any((BodyPartDef partDef) => bodyParts.Any((BodyPartRecord bodyPartRecord) => bodyPartRecord.def == partDef)))
		{
			return;
		}
		List<BodyPartDef> list = partsToAffect.Where((BodyPartDef partDef) => pawn.health.hediffSet.GetNotMissingParts().Any((BodyPartRecord bodyPart) => bodyPart.def == partDef)).ToList();
		HediffGiverUtility.TryApply(pawn, hediff, list, canAffectAnyLivePart, countToAffect);
		pawn.health.hediffSet.GetFirstHediffOfDef(hediff).Severity = severityAmount;
	}

	public override IEnumerable<string> ConfigErrors()
	{
		foreach (string item in base.ConfigErrors())
		{
			yield return item;
		}
		if (float.IsNaN(severityAmount))
		{
			yield return "severityAmount is not defined";
		}
		if (mtbHours < 0f)
		{
			yield return "mtbHours is not defined";
		}
	}
}
