using System;
using HarmonyLib;
using Verse;
using RimWorld;
using System.Text;
using RimWorld.QuestGen;
using System.Collections.Generic;
using RimWorld.Planet;

namespace GD3
{
	[HarmonyPatch(typeof(CompAnalyzable), "SendAppropriateProgressLetter")]
	public static class Analyze_Patch
	{
		public static bool Prefix(CompAnalyzable __instance, Pawn pawn, AnalysisDetails details)
		{
			if (details.id == 542148361)
			{
				if (details.Satisfied)
				{
					SendLetter(__instance.Props.completedLetterLabel, __instance.Props.completedLetter, __instance.Props.completedLetterDef, pawn);
				}
				return false;
			}
			return true;
		}

		private static void SendLetter(string label, string letter, LetterDef def, Pawn pawn)
		{
			if (!string.IsNullOrEmpty(label) && !string.IsNullOrEmpty(letter))
			{
				string formattedLetterString = GetFormattedLetterString(label, pawn);
				string formattedLetterString2 = GetFormattedLetterString(letter, pawn);
				Find.LetterStack.ReceiveLetter(formattedLetterString, formattedLetterString2, def);
			}
		}

		private static string GetFormattedLetterString(string text, Pawn pawn)
		{
			return text.Formatted(pawn.Named("PAWN")).Resolve();
		}
	}

    [HarmonyPatch(typeof(SignalManager), "SendSignal")]
	public static class SendSignal_Patch
	{
		public static void Postfix(Signal signal)
		{
			if (!GDSettings.DeveloperMode)
            {
				return;
            }
			Log.Message("Signal: tag=" + signal.tag.ToStringSafe() + " args=" + signal.args.Args.ToStringSafeEnumerable());
		}
	}

   [HarmonyPatch(typeof(QuestGen_End), "End")]
	public static class KeyCondition_Patch
	{
		public static void Prefix(this Quest quest)
		{
			if (quest.root.defName == "GD_Quest_Cluster_Fortress")
			{
				Find.World.GetComponent<MissionComponent>().keyGained = false;
			}
		}
	}

	[HarmonyPatch(typeof(Site), "CheckAllEnemiesDefeated")]
	public static class SiteDeafeated_Patch
	{
		public static bool Prefix(Site __instance)
		{
			if (!__instance.parts.NullOrEmpty() && __instance.parts[0].def.defName == "GD_Sitepart_Militor")
			{
				return false;
			}
			return true;
		}
	}
}