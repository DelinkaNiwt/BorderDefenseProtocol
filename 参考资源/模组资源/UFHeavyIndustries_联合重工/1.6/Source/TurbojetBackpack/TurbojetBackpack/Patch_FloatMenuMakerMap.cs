using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TurbojetBackpack;

[HarmonyPatch(typeof(FloatMenuMakerMap), "GetOptions")]
public static class Patch_FloatMenuMakerMap
{
	public static void Postfix(List<Pawn> selectedPawns, Vector3 clickPos, ref List<FloatMenuOption> __result)
	{
		if (selectedPawns.NullOrEmpty() || __result == null)
		{
			return;
		}
		IntVec3 c = IntVec3.FromVector3(clickPos);
		if (!c.InBounds(Find.CurrentMap) || c.Standable(Find.CurrentMap))
		{
			return;
		}
		List<Thing> thingList = c.GetThingList(Find.CurrentMap);
		for (int i = 0; i < thingList.Count; i++)
		{
			Thing thing = thingList[i];
			if (thing is IAttackTarget && thing.HostileTo(Faction.OfPlayer))
			{
				return;
			}
		}
		foreach (Pawn pawn in selectedPawns)
		{
			if (!pawn.Drafted || !TurbojetGlobal.IsFlightActive(pawn) || !TurbojetGlobal.IsValidDestination(pawn, pawn.Map, c))
			{
				continue;
			}
			string text = "GoHere".Translate();
			PawnPath pawnPath = null;
			try
			{
				TurbojetGlobal.IsCustomPathfinding = true;
				TurbojetGlobal.SkipReachabilityCheck = true;
				pawnPath = pawn.Map.pathFinder.FindPathNow(pawn.Position, c, TraverseParms.For(pawn, Danger.Deadly, TraverseMode.PassAllDestroyableThings));
				if (pawnPath == PawnPath.NotFound || !pawnPath.Found)
				{
					FloatMenuOption floatMenuOption = new FloatMenuOption(text + " (" + "NoPath".Translate() + ")", null);
					floatMenuOption.Disabled = true;
					__result.Add(floatMenuOption);
					continue;
				}
				Action action = delegate
				{
					Job job = JobMaker.MakeJob(JobDefOf.Goto, c);
					pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
					FleckMaker.Static(c, pawn.Map, FleckDefOf.FeedbackGoto);
				};
				FloatMenuOption floatMenuOption2 = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text, action, MenuOptionPriority.GoHere), pawn, c);
				floatMenuOption2.autoTakeable = true;
				floatMenuOption2.autoTakeablePriority = 90f;
				__result.Add(floatMenuOption2);
			}
			finally
			{
				TurbojetGlobal.IsCustomPathfinding = false;
				TurbojetGlobal.SkipReachabilityCheck = false;
				pawnPath?.ReleaseToPool();
			}
		}
	}
}
