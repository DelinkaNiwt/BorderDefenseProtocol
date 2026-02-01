using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using HarmonyLib;

namespace GD3
{
	[HarmonyPatch(typeof(Building_CommsConsole))]
	public static class CommsFloatMenu_Patch
	{
		[HarmonyTargetMethod]
		public static MethodInfo TargetMethod()
		{
			MethodInfo methodInfo = AccessTools.Method(typeof(Building_CommsConsole), "GetFloatMenuOptions", new Type[] { typeof(Pawn) });
			return methodInfo;
		}
		[HarmonyPostfix]
		public static void Postfix(Building_CommsConsole __instance, Pawn myPawn, ref IEnumerable<FloatMenuOption> __result)
		{
			List<FloatMenuOption> result = __result.ToList();

			if (!GDUtility.MainComponent.ClusterAssistanceAvailable(__instance.Map, out _))
			{
				return;
			}
			FloatMenuOption failureReason = GetFailureReason(__instance, myPawn);
			if (failureReason != null)
			{
				return;
			}
			try
			{
				if (GDUtility.MainComponent.artilleryStrikeCooldown > Find.TickManager.TicksGame)
                {
					string cooldown = (GDUtility.MainComponent.artilleryStrikeCooldown - Find.TickManager.TicksGame).ToStringTicksToDays();
					result.Add(new FloatMenuOption("GD.CannotCallArtillery".Translate() + ": " + "GD.ArtilleryCooldown".Translate(cooldown), null, Faction.OfMechanoids.def.FactionIcon, Faction.OfMechanoids.Color));
				}
                else
                {
					FloatMenuOption floatMenu = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("GD.CallArtillery".Translate(), delegate
					{
						Job job = JobMaker.MakeJob(GDDefOf.GD_CallArtillery, __instance);
						job.locomotionUrgency = LocomotionUrgency.Sprint;
						myPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
						PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.OpeningComms, KnowledgeAmount.Total);
					}, Faction.OfMechanoids.def.FactionIcon, Faction.OfMechanoids.Color, MenuOptionPriority.InitiateSocial), myPawn, __instance);
					result.Add(floatMenu);
				}
			}
			catch (Exception e)
			{
				Log.Error("GD5 - add comms console float menu error: " + e);
			}

			__result = result;
		}

		private static FloatMenuOption GetFailureReason(Building_CommsConsole thing, Pawn myPawn)
		{
			if (!myPawn.CanReach(thing, PathEndMode.InteractionCell, Danger.Some))
			{
				return new FloatMenuOption("CannotUseNoPath".Translate(), null);
			}
			if (thing.Spawned && thing.Map.gameConditionManager.ElectricityDisabled(thing.Map))
			{
				return new FloatMenuOption("CannotUseSolarFlare".Translate(), null);
			}
			CompPowerTrader powerComp = thing.GetComp<CompPowerTrader>();
			if (powerComp != null && !powerComp.PowerOn)
			{
				return new FloatMenuOption("CannotUseNoPower".Translate(), null);
			}
			if (!myPawn.health.capacities.CapableOf(PawnCapacityDefOf.Talking))
			{
				return new FloatMenuOption("CannotUseReason".Translate("IncapableOfCapacity".Translate(PawnCapacityDefOf.Talking.label, myPawn.Named("PAWN"))), null);
			}
			if (!thing.CanUseCommsNow)
			{
				Log.Error(string.Concat(myPawn, " could not use comm console for unknown reason."));
				return new FloatMenuOption("Cannot use now", null);
			}
			return null;
		}
	}
}
