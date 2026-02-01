using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace NCL;

[StaticConstructorOnStartup]
public static class IFCHarmony
{
	static IFCHarmony()
	{
		Harmony harmony = new Harmony("IFC.Patch");
		harmony.PatchAll();
		harmony.Patch(AccessTools.Method(typeof(Pawn_EquipmentTracker), "GetGizmos"), null, new HarmonyMethod(typeof(IFCHarmony), "GetExtraEquipmentGizmosPassThrough"));
		harmony.Patch(AccessTools.Method(typeof(Pawn_EquipmentTracker), "EquipmentTrackerTick"), null, new HarmonyMethod(typeof(IFCHarmony), "IFCPostTickEquipment"));
	}

	public static void IFCPostTickEquipment(Pawn_EquipmentTracker __instance)
	{
		List<ThingWithComps> allEquipmentListForReading = __instance.AllEquipmentListForReading;
		for (int i = 0; i < allEquipmentListForReading.Count; i++)
		{
			allEquipmentListForReading[i].GetComp<CompFormChange>()?.CooldownTick();
		}
	}

	public static IEnumerable<Gizmo> GetExtraEquipmentGizmosPassThrough(IEnumerable<Gizmo> values, Pawn_EquipmentTracker __instance)
	{
		foreach (Gizmo value in values)
		{
			yield return value;
		}
		if ((!__instance.pawn.IsColonistPlayerControlled && (!__instance.pawn.RaceProps.IsMechanoid || __instance.pawn.Faction != Faction.OfPlayer)) || !PawnAttackGizmoUtility.CanShowEquipmentGizmos())
		{
			yield break;
		}
		List<ThingWithComps> list = __instance.AllEquipmentListForReading;
		for (int i = 0; i < list.Count; i++)
		{
			CompFormChange compFormChange = list[i].TryGetComp<CompFormChange>();
			if (compFormChange == null)
			{
				continue;
			}
			foreach (Gizmo item in compFormChange.HeldGizmos(__instance.pawn))
			{
				yield return item;
			}
		}
	}
}
