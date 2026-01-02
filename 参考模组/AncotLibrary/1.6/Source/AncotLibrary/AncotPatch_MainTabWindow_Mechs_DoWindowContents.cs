using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

[HarmonyPatch(typeof(MainTabWindow_Mechs))]
[HarmonyPatch("DoWindowContents")]
public static class AncotPatch_MainTabWindow_Mechs_DoWindowContents
{
	[HarmonyPostfix]
	public static void Postfix(Rect rect)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Invalid comparison between Unknown and I4
		if ((int)Event.current.type != 8 && AncotLibrarySettings.drone_TabAvailable != TabAvailable.Menu)
		{
			Rect butRect = new Rect(rect.x + 280f, rect.y, 32f, 32f).ContractedBy(0.8f);
			if (MechanitorUtility.MechsInPlayerFaction().Any((Pawn p) => p.TryGetComp<CompDrone>() != null) && Widgets.ButtonImage(butRect, AncotLibraryIcon.Drone, doMouseoverSound: true, "Ancot.DroneTabTip".Translate()))
			{
				Find.MainTabsRoot.SetCurrentTab(AncotDefOf.Ancot_DronesTab);
			}
		}
	}
}
