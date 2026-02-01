using System;
using HarmonyLib;
using Verse;
using RimWorld;
using System.Collections.Generic;

namespace GD3
{
	[HarmonyPatch(typeof(Verb_MeleeAttack), "GetDodgeChance")]
	public static class Dodge_Patch
	{
		public static void Postfix(Verb_MeleeAttack __instance, LocalTargetInfo target, ref float __result)
		{
			if (target.Thing as Pawn == null)
            {
				return;
            }
			Pawn p = target.Thing as Pawn;
			HediffWithComps h = p.health.hediffSet.GetFirstHediffOfDef(GDDefOf.Reinforce_Dodge) as HediffWithComps;
			if (h == null)
            {
				return;
            }
			Pawn attacker = __instance.CasterPawn;
			if (attacker == null)
            {
				return;
            }
			if (attacker.RaceProps.Animal)
            {
				__result = 1f;
				return;
            }
			if (attacker.RaceProps.IsMechanoid)
			{
				HediffComp_Dodge comp = h.TryGetComp<HediffComp_Dodge>();
				if (comp == null)
                {
					return;
                }
				__result = comp.Props.DodgeChanceFacingMechanoids;
				return;
			}
			if (attacker.RaceProps.Humanlike)
            {
				if (attacker.skills == null)
                {
					return;
                }
				if (attacker.skills.GetSkill(SkillDefOf.Melee).Level < 18)
                {
					__result = 1.0f;
                }
                else if (attacker.skills.GetSkill(SkillDefOf.Melee).Level >= 18)
                {
					__result += 0.45f;
                }
            }
		}
	}
}