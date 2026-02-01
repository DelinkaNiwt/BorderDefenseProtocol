using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace GD3
{
    [HarmonyPatch(typeof(CompCerebrexCore), "DeactivateCore")]
    public static class CompCerebrexCore_Patch
    {
        public static bool Prefix(CompCerebrexCore __instance, bool scavenging)
        {
            if (!scavenging)
            {
                Pawn interactedPawn = (Pawn)typeof(CompCerebrexCore).GetField("interactedPawn", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);

				Find.TickManager.Pause();
				QuestUtility.SendQuestTargetSignals(__instance.parent.questTags, "CoreDefeated", __instance.parent.Named("SUBJECT"));
				(Find.Scenario.AllParts.FirstOrDefault((ScenPart x) => x is ScenPart_PursuingMechanoids) as ScenPart_PursuingMechanoids)?.Notify_QuestCompleted();

				Thing thing = ThingMaker.MakeThing(ThingDefOf.Apparel_CerebrexNode);
				GenPlace.TryPlaceThing(thing, __instance.parent.Position, __instance.parent.Map, ThingPlaceMode.Near);
				
				Map map = __instance.parent.Map;
				Thing.allowDestroyNonDestroyable = true;
				__instance.parent.Destroy();
				Thing.allowDestroyNonDestroyable = false;
				GenSpawn.Spawn(ThingDefOf.CerebrexCore_Destroyed, __instance.parent.Position, map);
				Find.LetterStack.ReceiveLetter("CerebrexCoreDestroyedLetterLabel".Translate(), "CerebrexCoreDestroyedLetterText".Translate(interactedPawn.Named("PAWN")) + "\n\n" + "GD.CerebrexDestroyedTip".Translate(), LetterDefOf.PositiveEvent, thing);
				if (Faction.OfMechanoids != null)
				{
					GDSettings.threatFactorPostfix = 0.2f;
				}
				foreach (Pawn item in PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_OfPlayerFaction)
				{
					item.needs?.mood?.thoughts?.memories?.TryGainMemory(ThoughtDefOf.DestroyedMechhive);
				}
				GameVictoryUtility.ShowCredits("OdysseyDestroyedCredits".Translate(interactedPawn.Named("PAWN")), SongDefOf.OdysseyCreditsSong, exitToMainMenu: false, 0f);
				return false;
			}
            return true;
        }
    }

	[HarmonyPatch(typeof(CompAbilityEffect_DeactivateMechanoid), "Valid")]
	public static class DeactivateMechanoid_Patch
	{
		public static bool Prefix(CompAbilityEffect_DeactivateMechanoid __instance, LocalTargetInfo target, ref bool __result)
		{
			Pawn pawn = target.Pawn;
			if (pawn == null)
			{
				return true;
			}
			if (pawn.IsSpecialMech())
			{
				__result = false;
				return false;
			}
			return true;
		}
	}
}
