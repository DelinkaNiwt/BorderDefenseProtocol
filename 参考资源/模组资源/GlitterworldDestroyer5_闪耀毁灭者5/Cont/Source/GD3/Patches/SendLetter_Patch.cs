using System;
using HarmonyLib;
using Verse;
using RimWorld;

namespace GD3
{
	[HarmonyPatch(typeof(HediffGiver), "SendLetter")]
	public static class SendLetter_Patch
	{
		public static bool Prefix(Pawn pawn, Hediff cause, HediffGiver __instance)
		{
			if (pawn != null && __instance.hediff != null && __instance.hediff.GetModExtension<ModExtension_NotSendLetter>() != null)
			{
				return false;
			}
			else
			{
				return true;
			}
		}
	}
}