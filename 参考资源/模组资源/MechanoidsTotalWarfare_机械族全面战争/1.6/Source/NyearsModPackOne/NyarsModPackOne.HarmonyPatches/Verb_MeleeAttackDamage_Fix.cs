using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace NyarsModPackOne.HarmonyPatches;

[StaticConstructorOnStartup]
public class Verb_MeleeAttackDamage_Fix
{
	[HarmonyPatch(typeof(Verb_MeleeAttackDamage), "ApplyMeleeDamageToTarget")]
	private static class ApplyMeleeDamageToTarget_PreFix
	{
		[HarmonyPrefix]
		private static void Prefix(Verb_MeleeAttackDamage __instance, LocalTargetInfo target)
		{
			ModExtension_EffecterOnMelee modExtension_EffecterOnMelee = __instance.Caster?.def.GetModExtension<ModExtension_EffecterOnMelee>();
			if (modExtension_EffecterOnMelee == null)
			{
				return;
			}
			Vector3 vector = __instance.Caster.TrueCenter();
			IntVec3 position = __instance.Caster.Position;
			Vector3 centerVector = target.CenterVector3;
			IntVec3 cell = target.Cell;
			Map map = __instance.Caster.Map;
			float num = 0.2f;
			IEnumerable<FleckDef> flecksAtTarget = modExtension_EffecterOnMelee.flecksAtTarget;
			foreach (FleckDef item in flecksAtTarget ?? Enumerable.Empty<FleckDef>())
			{
				Vector3 vector2 = new Vector3(Rand.Range(0f - num, num), 0f, Rand.Range(0f - num, num));
				FleckCreationData dataStatic = FleckMaker.GetDataStatic(centerVector + vector2, map, item);
				dataStatic.rotation = Rand.Range(0f, 360f);
				map.flecks.CreateFleck(dataStatic);
			}
			IEnumerable<EffecterDef> effectersAtTarget = modExtension_EffecterOnMelee.effectersAtTarget;
			foreach (EffecterDef item2 in effectersAtTarget ?? Enumerable.Empty<EffecterDef>())
			{
				Effecter effecter = item2.Spawn();
				effecter.Trigger(new TargetInfo(cell, map), new TargetInfo(cell, map), 300);
			}
			IEnumerable<ThingDef> motesAtTarget = modExtension_EffecterOnMelee.motesAtTarget;
			foreach (ThingDef item3 in motesAtTarget ?? Enumerable.Empty<ThingDef>())
			{
				Vector3 vector3 = new Vector3(Rand.Range(0f - num, num), 0f, Rand.Range(0f - num, num));
				MoteMaker.MakeStaticMote(centerVector + vector3, map, item3, 1f, makeOffscreen: false, Rand.Range(0f, 360f));
			}
			flecksAtTarget = modExtension_EffecterOnMelee.flecksAtCaster;
			foreach (FleckDef item4 in flecksAtTarget ?? Enumerable.Empty<FleckDef>())
			{
				Vector3 vector4 = new Vector3(Rand.Range(0f - num, num), 0f, Rand.Range(0f - num, num));
				FleckCreationData dataStatic2 = FleckMaker.GetDataStatic(vector + vector4, map, item4);
				dataStatic2.rotation = Rand.Range(0f, 360f);
				map.flecks.CreateFleck(dataStatic2);
			}
			effectersAtTarget = modExtension_EffecterOnMelee.effectersAtCaster;
			foreach (EffecterDef item5 in effectersAtTarget ?? Enumerable.Empty<EffecterDef>())
			{
				Effecter effecter2 = item5.Spawn();
				effecter2.Trigger(new TargetInfo(position, map), new TargetInfo(position, map), 300);
			}
			motesAtTarget = modExtension_EffecterOnMelee.motesAtCaster;
			foreach (ThingDef item6 in motesAtTarget ?? Enumerable.Empty<ThingDef>())
			{
				Vector3 vector5 = new Vector3(Rand.Range(0f - num, num), 0f, Rand.Range(0f - num, num));
				MoteMaker.MakeStaticMote(vector + vector5, map, item6, 1f, makeOffscreen: false, Rand.Range(0f, 360f));
			}
			flecksAtTarget = modExtension_EffecterOnMelee.flecksLinkLine;
			foreach (FleckDef item7 in flecksAtTarget ?? Enumerable.Empty<FleckDef>())
			{
				FleckMaker.ConnectingLine(vector, centerVector, item7, map);
			}
			motesAtTarget = modExtension_EffecterOnMelee.motesLinkLine;
			foreach (ThingDef item8 in motesAtTarget ?? Enumerable.Empty<ThingDef>())
			{
				MoteMaker.MakeConnectingLine(vector, centerVector, item8, map);
			}
		}
	}
}
