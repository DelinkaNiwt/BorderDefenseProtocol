using HarmonyLib;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded;

[HarmonyPatch(typeof(Pawn), "Kill")]
public static class Pawn_Kill_Patch
{
	private static bool Prefix(Pawn __instance)
	{
		if (__instance.health.hediffSet.GetFirstHediffOfDef(VPE_DefOf.VPE_DeathShield) != null)
		{
			return false;
		}
		return true;
	}

	private static void Postfix(Pawn __instance, DamageInfo? dinfo, Hediff exactCulprit = null)
	{
		if (!__instance.Dead)
		{
			return;
		}
		if (dinfo.HasValue && dinfo.Value.Instigator is Pawn pawn)
		{
			Hediff firstHediffOfDef = pawn.health.hediffSet.GetFirstHediffOfDef(VPE_DefOf.VPE_ControlledFrenzy);
			Hediff_Ability val = (Hediff_Ability)(object)((firstHediffOfDef is Hediff_Ability) ? firstHediffOfDef : null);
			if (val != null)
			{
				pawn.psychicEntropy.TryAddEntropy(-10f);
				((Hediff)(object)val).TryGetComp<HediffComp_Disappears>().ticksToDisappear = val.ability.GetDurationForPawn();
			}
		}
		Hediff firstHediffOfDef2 = __instance.health.hediffSet.GetFirstHediffOfDef(VPE_DefOf.VPE_IceBlock);
		if (firstHediffOfDef2 != null)
		{
			__instance.health.RemoveHediff(firstHediffOfDef2);
		}
	}
}
