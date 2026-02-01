using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimTalk.Data;
using RimTalk.UI;
using RimTalk.Util;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimTalk.Patch;

[HarmonyPatch(typeof(Pawn), "GetGizmos")]
public static class PawnGizmoPatch
{
	[HarmonyPostfix]
	public static void Postfix(Pawn __instance, ref IEnumerable<Gizmo> __result)
	{
		if (__instance == null || !Settings.Get().AllowCustomConversation || Settings.Get().PlayerDialogueMode == Settings.PlayerDialogueMode.Disabled || !__instance.Spawned || __instance.Dead || !__instance.IsTalkEligible())
		{
			return;
		}
		Selector selector = Find.Selector;
		if (selector.SelectedPawns.Count != 1)
		{
			return;
		}
		List<Gizmo> list = ((__result != null) ? __result.ToList() : new List<Gizmo>());
		Command_Action cmd = new Command_Action
		{
			defaultLabel = "RimTalk.Gizmo.ChatWithTarget".Translate(__instance.LabelShort),
			defaultDesc = "RimTalk.Gizmo.ChatWithTargetDesc".Translate(__instance.LabelShort),
			icon = ContentFinder<Texture2D>.Get("UI/ChatGizmo"),
			action = delegate
			{
				Pawn player = global::RimTalk.Data.Cache.GetPlayer();
				if (player != null)
				{
					Find.WindowStack.Add(new CustomDialogueWindow(player, __instance));
				}
			}
		};
		list.Add(cmd);
		__result = list;
	}
}
