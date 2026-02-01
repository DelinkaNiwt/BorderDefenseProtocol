using System;
using HarmonyLib;
using Verse;
using RimWorld;
using UnityEngine;
using Verse.Sound;

namespace GD3
{
	[HarmonyPatch(typeof(ThingWithComps), "Comps_PostDraw")]
	public static class Shield_Patch_Draw
	{
		public static void Postfix(ThingWithComps __instance)
		{
			Pawn thing = __instance as Pawn;
			if (thing != null && thing.health?.hediffSet != null)
            {
				HediffWithComps hediff = thing.health.hediffSet.GetFirstHediffOfDef(GDDefOf.GD_BlackShield) as HediffWithComps;
				if (hediff != null)
                {
					Vector3 drawPos = __instance.DrawPos;
					drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
					float angle = Rand.Range(0, 360);
					Matrix4x4 matrix = default(Matrix4x4);
					matrix.SetTRS(drawPos, Quaternion.AngleAxis(angle, Vector3.up), new Vector3(1.8f, 1.8f, 1.8f));
					Graphics.DrawMesh(MeshPool.plane10, matrix, MaterialPool.MatFrom("Other/ShieldBubble", ShaderDatabase.Transparent), 0);
				}
            }
		}
	}

	[HarmonyPatch(typeof(ThingWithComps), "PreApplyDamage")]
	public static class Shield_Patch_Absorb
	{
		public static bool Prefix(ThingWithComps __instance, ref DamageInfo dinfo, out bool absorbed)
		{
			absorbed = false;
			Pawn thing = __instance as Pawn;
			if (thing != null && thing.health?.hediffSet != null)
			{
				HediffWithComps hediff = thing.health.hediffSet.GetFirstHediffOfDef(GDDefOf.GD_BlackShield) as HediffWithComps;
				if (hediff != null)
				{
					absorbed = true;
					SoundDefOf.EnergyShield_AbsorbDamage.PlayOneShot(new TargetInfo(thing.Position, thing.Map));
					Vector3 impactAngleVect = Vector3Utility.HorizontalVectorFromAngle(dinfo.Angle);
					Vector3 loc = thing.TrueCenter() + impactAngleVect.RotatedBy(180f) * 0.5f;
					float num = Mathf.Min(10f, 2f + dinfo.Amount / 10f);
					FleckMaker.Static(loc, thing.Map, FleckDefOf.ExplosionFlash, num);
					int num2 = (int)num;
					for (int i = 0; i < num2; i++)
					{
						FleckMaker.ThrowDustPuff(loc, thing.Map, Rand.Range(0.8f, 1.2f));
					}
					return false;
				}
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(CompShield), "Break")]
	public static class Shield_Patch_PolarizedLight
	{
		public static void Postfix(CompShield __instance)
		{
			Apparel apparel = __instance.parent as Apparel;
			Pawn pawn = apparel?.Wearer;
			if (apparel?.def.defName == "GD_PolarizedLight" && pawn != null)
			{
				Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(GDDefOf.GD_BlackShield, false);
				if (hediff == null)
				{
					hediff = pawn.health.AddHediff(GDDefOf.GD_BlackShield, pawn.health.hediffSet.GetBrain(), null, null);
					hediff.Severity = 1f;
				}
				HediffComp_Disappears hediffComp_Disappears = hediff.TryGetComp<HediffComp_Disappears>();
				if (hediffComp_Disappears != null)
				{
					hediffComp_Disappears.ticksToDisappear = 600;
				}
				FleckMaker.Static(pawn.TrueCenter(), pawn.Map, FleckDefOf.BroadshieldActivation, 0.6f);
				SoundDefOf.Broadshield_Startup.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map, false));
			}
		}
	}
}