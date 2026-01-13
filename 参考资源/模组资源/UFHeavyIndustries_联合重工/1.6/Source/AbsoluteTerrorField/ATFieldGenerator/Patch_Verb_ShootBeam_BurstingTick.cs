using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ATFieldGenerator;

[HarmonyPatch(typeof(Verb_ShootBeam), "BurstingTick")]
public static class Patch_Verb_ShootBeam_BurstingTick
{
	public static bool Prefix(Verb_ShootBeam __instance)
	{
		if (__instance.Caster == null || __instance.Caster.Map == null)
		{
			return true;
		}
		Vector3 drawPos = __instance.Caster.DrawPos;
		Vector3 interpolatedPosition = __instance.InterpolatedPosition;
		List<Comp_AbsoluteTerrorField> activeFields = ATFieldManager.Get(__instance.Caster.Map).activeFields;
		if (activeFields == null || activeFields.Count == 0)
		{
			return true;
		}
		for (int i = 0; i < activeFields.Count; i++)
		{
			Comp_AbsoluteTerrorField comp_AbsoluteTerrorField = activeFields[i];
			if (comp_AbsoluteTerrorField.CheckBurstingTickIntercept(__instance, drawPos, interpolatedPosition))
			{
				Vector3 b = comp_AbsoluteTerrorField.parent.Position.ToVector3Shifted();
				Vector3 normalized = (interpolatedPosition - drawPos).normalized;
				float num = Vector3.Distance(drawPos, b);
				float num2 = num - comp_AbsoluteTerrorField.radius;
				Vector3 vector = drawPos + normalized * num2;
				IntVec3 cell = vector.ToIntVec3();
				MoteDualAttached moteDualAttached = (MoteDualAttached)AccessTools.Field(typeof(Verb_ShootBeam), "mote").GetValue(__instance);
				if (moteDualAttached != null)
				{
					Vector3 offsetA = normalized.Yto0() * __instance.verbProps.beamStartOffset;
					Vector3 offsetB = vector - cell.ToVector3Shifted();
					moteDualAttached.UpdateTargets(new TargetInfo(__instance.Caster.Position, __instance.Caster.Map), new TargetInfo(cell, __instance.Caster.Map), offsetA, offsetB);
					moteDualAttached.Maintain();
				}
				if (__instance.verbProps.beamGroundFleckDef != null && Rand.Chance(__instance.verbProps.beamFleckChancePerTick))
				{
					FleckMaker.Static(vector, __instance.Caster.Map, __instance.verbProps.beamGroundFleckDef);
				}
				int num3 = (int)AccessTools.Field(typeof(Verb_ShootBeam), "ticksToNextPathStep").GetValue(__instance);
				AccessTools.Field(typeof(Verb_ShootBeam), "ticksToNextPathStep").SetValue(__instance, num3 - 1);
				((Sustainer)AccessTools.Field(typeof(Verb_ShootBeam), "sustainer").GetValue(__instance))?.Maintain();
				return false;
			}
		}
		return true;
	}
}
