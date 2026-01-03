using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace AncotLibrary;

[HarmonyPatch(typeof(Pawn))]
[HarmonyPatch("GetGizmos")]
public static class AncotPatch_Pawn_GetGizmos
{
	[HarmonyPostfix]
	public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn __instance)
	{
		foreach (Gizmo item in __result)
		{
			yield return item;
		}
		if (__instance.equipment == null)
		{
			yield break;
		}
		ThingWithComps primaryEquipment = __instance.equipment.Primary;
		if (primaryEquipment == null || primaryEquipment.AllComps.NullOrEmpty())
		{
			yield break;
		}
		foreach (ThingComp comp in primaryEquipment.AllComps)
		{
			if (!(comp is IPawnWeaponGizmoProvider provider))
			{
				continue;
			}
			foreach (Gizmo weaponGizmo in provider.GetWeaponGizmos())
			{
				yield return weaponGizmo;
			}
		}
	}
}
