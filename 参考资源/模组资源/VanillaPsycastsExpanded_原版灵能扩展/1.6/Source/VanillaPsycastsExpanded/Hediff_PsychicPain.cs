using RimWorld;
using UnityEngine;
using Verse;

namespace VanillaPsycastsExpanded;

public class Hediff_PsychicPain : HediffWithComps
{
	public override float PainOffset => Mathf.Max(pawn.GetStatValue(StatDefOf.PsychicSensitivity) - 0.8f, 0f);
}
