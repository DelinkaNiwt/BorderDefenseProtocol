using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace NCL;

[HarmonyPatch(typeof(LetterStack))]
[HarmonyPatch("ReceiveLetter")]
public static class Patch_LetterStack_ReceiveLetter
{
	private static readonly LetterDef[] NotificationDefs = new LetterDef[2]
	{
		LetterDefOf.NegativeEvent,
		LetterDefOf.Death
	};

	public static bool Prefix(Letter letter)
	{
		if (NotificationDefs.Contains(letter.def))
		{
			Pawn affectedPawn = TryGetAffectedPawn(letter);
			if (affectedPawn != null && affectedPawn.IsColonist && HasNoNotificationHediff(affectedPawn))
			{
				return false;
			}
		}
		return true;
	}

	private static Pawn TryGetAffectedPawn(Letter letter)
	{
		if (letter.lookTargets != null && letter.lookTargets.PrimaryTarget.Thing is Pawn directTarget)
		{
			return directTarget;
		}
		return null;
	}

	private static bool HasNoNotificationHediff(Pawn pawn)
	{
		if (pawn?.health?.hediffSet == null)
		{
			return false;
		}
		foreach (Hediff hediff2 in pawn.health.hediffSet.hediffs)
		{
			if (hediff2?.def?.GetModExtension<HediffExtension_NoNotification>() != null)
			{
				return true;
			}
		}
		return false;
	}
}
