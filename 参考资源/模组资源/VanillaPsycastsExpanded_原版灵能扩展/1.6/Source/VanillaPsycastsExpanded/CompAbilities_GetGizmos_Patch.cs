using System.Collections.Generic;
using HarmonyLib;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded;

[HarmonyPatch(typeof(CompAbilities), "GetGizmos")]
public static class CompAbilities_GetGizmos_Patch
{
	public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> gizmos, CompAbilities __instance)
	{
		Pawn pawn = ((ThingComp)(object)__instance).parent as Pawn;
		Hediff_PsycastAbilities psycasts = pawn.Psycasts();
		if (psycasts != null && pawn != null && pawn.Drafted)
		{
			foreach (Gizmo psySetGizmo in psycasts.GetPsySetGizmos())
			{
				yield return psySetGizmo;
			}
		}
		foreach (Gizmo gizmo in gizmos)
		{
			if (psycasts != null)
			{
				Command_Ability val = (Command_Ability)(object)((gizmo is Command_Ability) ? gizmo : null);
				if (val != null && ((Def)(object)val.ability.def).HasModExtension<AbilityExtension_Psycast>())
				{
					if (psycasts.ShouldShow(val.ability))
					{
						yield return (Gizmo)(object)val;
					}
					continue;
				}
			}
			yield return gizmo;
		}
	}
}
