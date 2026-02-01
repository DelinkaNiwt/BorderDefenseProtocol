using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VanillaPsycastsExpanded.Nightstalker;

[HarmonyPatch]
public class Hediff_Darkvision : HediffWithComps
{
	public static HashSet<Pawn> DarkvisionPawns = new HashSet<Pawn>();

	[HarmonyPatch(typeof(ThoughtUtility), "NullifyingHediff")]
	[HarmonyPostfix]
	public static void NullDarkness(ThoughtDef def, Pawn pawn, ref Hediff __result)
	{
		if (def == VPE_DefOf.EnvironmentDark && __result == null && DarkvisionPawns.Contains(pawn))
		{
			__result = pawn.health.hediffSet.hediffs.OfType<Hediff_Darkvision>().FirstOrDefault();
		}
	}

	[HarmonyPatch(typeof(StatPart_Glow), "ActiveFor")]
	[HarmonyPostfix]
	public static void NoDarkPenalty(Thing t, ref bool __result)
	{
		if (__result && t is Pawn item && DarkvisionPawns.Contains(item))
		{
			__result = false;
		}
	}

	public override void PostAdd(DamageInfo? dinfo)
	{
		base.PostAdd(dinfo);
		DarkvisionPawns.Add(pawn);
		foreach (BodyPartRecord item in pawn.RaceProps.body.AllParts.Where((BodyPartRecord part) => part.def == BodyPartDefOf.Eye))
		{
			pawn.health.AddHediff(VPE_DefOf.VPE_Darkvision_Display, item);
		}
	}

	public override void PostRemoved()
	{
		base.PostRemoved();
		DarkvisionPawns.Remove(pawn);
		Hediff firstHediffOfDef;
		while ((firstHediffOfDef = pawn.health.hediffSet.GetFirstHediffOfDef(VPE_DefOf.VPE_Darkvision_Display)) != null)
		{
			pawn.health.RemoveHediff(firstHediffOfDef);
		}
	}
}
