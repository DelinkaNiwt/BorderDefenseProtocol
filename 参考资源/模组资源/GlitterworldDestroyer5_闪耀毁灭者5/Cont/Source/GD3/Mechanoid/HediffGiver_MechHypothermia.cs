using System;
using RimWorld;
using Verse;
using UnityEngine;

namespace GD3
{
	public class HediffGiver_MechHypothermia : Verse.HediffGiver
	{
		public Verse.HediffDef hediffInsectoid;

		public override void OnIntervalPassed(Verse.Pawn pawn, Verse.Hediff cause)
		{
			float ambientTemperature = pawn.AmbientTemperature;
			Verse.FloatRange floatRange = pawn.ComfortableTemperatureRange();
			Verse.FloatRange floatRange2 = pawn.SafeTemperatureRange();
			Verse.HediffSet hediffSet = pawn.health.hediffSet;
			Verse.HediffDef hediffDef = ((pawn.RaceProps.FleshType == RimWorld.FleshTypeDefOf.Insectoid) ? hediffInsectoid : hediff);
			Verse.Hediff firstHediffOfDef = hediffSet.GetFirstHediffOfDef(hediffDef);
			if (firstHediffOfDef == null)
			{
				return;
			}
			if (ambientTemperature > floatRange.min)
			{
				float value = firstHediffOfDef.Severity * 0.027f;
				value = UnityEngine.Mathf.Clamp(value, 0.001f, 0.0015f);
				firstHediffOfDef.Severity -= value;
			}
		}
	}
}

