using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace TOT_DLL_test;

[StaticConstructorOnStartup]
internal class HarmonyPatches
{
	internal static class Harmony_CheckForFreeInterceptBetween
	{
		public static bool Prefix(Projectile __instance, Vector3 lastExactPos, Vector3 newExactPos, ref bool __result)
		{
			if (lastExactPos == newExactPos)
			{
				return false;
			}
			if (CMC_Def.CMCShieldGenerator == null)
			{
				return true;
			}
			List<Thing> list = __instance.Map.listerThings.ThingsOfDef(CMC_Def.CMCShieldGenerator);
			for (int i = 0; i < list.Count; i++)
			{
				try
				{
					if (list[i] is Building_FRShield thing && thing.TryGetComp<CompFullProjectileInterceptor>().CheckIntercept(__instance, lastExactPos, newExactPos))
					{
						__instance.Destroy();
						__result = true;
						return false;
					}
				}
				catch (Exception)
				{
				}
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(PawnRenderUtility), "DrawEquipmentAiming")]
	private class HarmonyPatch_PawnWeaponRenderer
	{
		public static bool Prefix(Thing eq, Vector3 drawLoc, float aimAngle)
		{
			if (eq != null && eq.TryGetComp<Comp_WeaponRenderStatic>() != null && eq.TryGetComp<CompEquippable>().ParentHolder != null)
			{
				DrawWeaponExtraEquipped.DrawExtraMatStatic(eq, drawLoc, aimAngle);
				return false;
			}
			return true;
		}
	}

	[StaticConstructorOnStartup]
	[HarmonyPatch(typeof(MapPawns), "PlayerEjectablePodHolder")]
	private static class PlayerEjectablePodHolder_PostFix
	{
		[HarmonyPostfix]
		public static void PostFix(Thing thing, ref IThingHolder __result)
		{
			if (thing is SkillDummy_Sword skillDummy_Sword && skillDummy_Sword.innerContainer.Any)
			{
				__result = thing as IThingHolder;
			}
		}
	}

	[StaticConstructorOnStartup]
	[HarmonyPatch(typeof(PawnsArrivalModeWorker_CenterDrop), "TryResolveRaidSpawnCenter", null)]
	public static class Harmony_CenterDrop_TryResolveRaidSpawnCenter
	{
		public static void Prefix(IncidentParms parms)
		{
			if (parms.target is Map map)
			{
				List<Thing> list = map.listerThings.ThingsOfDef(CMC_Def.CMC_CICAESA_Radar);
				if (list != null)
				{
					parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeDrop;
					parms.spawnCenter = DropCellFinder.FindRaidDropCenterDistant(map);
					parms.raidNeverFleeIndividual = true;
					parms.podOpenDelay = 14;
					parms.pointMultiplier = 1.4f;
				}
			}
		}
	}

	[HarmonyPatch(typeof(VerbProperties), "get_Ranged")]
	public static class VerbProps_Patch
	{
		public static bool Prefix(VerbProperties __instance, ref bool __result)
		{
			if (__instance.verbClass == typeof(Verb_ShootSwitchFire))
			{
				__result = true;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(MapParent), "CheckRemoveMapNow")]
	public static class MapParent_CheckRemoveMapNow_Patch
	{
		public static bool Prefix(MapParent __instance)
		{
			if (__instance == GameComponent_CeleTech.Instance.ASEA_observedMap)
			{
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Settlement), "ShouldRemoveMapNow")]
	public static class SettlementShouldRemoveMapPatch
	{
		[HarmonyPostfix]
		public static void Postfix(ref bool __result, Settlement __instance)
		{
			if (__result && __instance == GameComponent_CeleTech.Instance.ASEA_observedMap)
			{
				__result = false;
			}
		}
	}

	static HarmonyPatches()
	{
		Harmony harmony = new Harmony("TOT.CMC.Weaponry");
		harmony.PatchAll(Assembly.GetExecutingAssembly());
		harmony.Patch(AccessTools.Method(typeof(Projectile), "CheckForFreeInterceptBetween"), new HarmonyMethod(typeof(Harmony_CheckForFreeInterceptBetween), "Prefix"));
		Log.Message("projectileinterceptor patched");
		harmony.Patch(AccessTools.Method(typeof(PawnRenderUtility), "DrawEquipmentAiming"), new HarmonyMethod(typeof(HarmonyPatch_PawnWeaponRenderer), "Prefix"));
		Log.Message("GunRender patched");
	}
}
