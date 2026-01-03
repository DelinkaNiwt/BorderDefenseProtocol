using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Milira;

[HarmonyPatch(typeof(MainTabWindow_Mechs))]
[HarmonyPatch("DoWindowContents")]
public static class Milian_MainTabWindow_Mechs_DoWindowContents_Patch
{
	[HarmonyPostfix]
	public static void Postfix(Rect rect)
	{
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Invalid comparison between Unknown and I4
		Rect butRect = new Rect(rect.x + 245f, rect.y, 32f, 32f).ContractedBy(0.8f);
		List<Pawn> list = (from p in MechanitorUtility.MechsInPlayerFaction()
			where MilianUtility.IsMilian(p)
			select p).ToList();
		if (!list.NullOrEmpty() && (int)MiliraRaceSettings.TabAvailable_MilianConfig != 1 && Widgets.ButtonImage(butRect, MiliraIcon.MilianConfig, doMouseoverSound: true, "Milira.MilianConfigTip".Translate()))
		{
			Find.MainTabsRoot.SetCurrentTab(MiliraDefOf.Milian_Config);
		}
		foreach (Pawn item in list)
		{
			if (MilianUtility.IsMilian(item))
			{
				item?.Drawer?.renderer?.renderTree?.SetDirty();
			}
		}
	}
}
