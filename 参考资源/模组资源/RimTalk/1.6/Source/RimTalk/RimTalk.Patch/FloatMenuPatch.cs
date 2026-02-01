using System.Collections.Generic;
using HarmonyLib;
using RimTalk.Data;
using RimTalk.UI;
using RimTalk.Util;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RimTalk.Patch;

[HarmonyPatch(typeof(FloatMenuMakerMap), "GetOptions")]
public static class FloatMenuPatch
{
	private const int ClickRadiusCells = 1;

	[HarmonyPostfix]
	public static void Postfix(List<Pawn> selectedPawns, Vector3 clickPos, FloatMenuContext context, ref List<FloatMenuOption> __result)
	{
		Pawn pawn = ((selectedPawns != null && selectedPawns.Count == 1) ? selectedPawns[0] : null);
		TryAddTalkOption(__result, pawn, clickPos);
	}

	private static void TryAddTalkOption(List<FloatMenuOption> result, Pawn selectedPawn, Vector3 clickPos)
	{
		if (result == null || !Settings.Get().AllowCustomConversation || selectedPawn == null || selectedPawn.Drafted || !selectedPawn.Spawned || selectedPawn.Dead)
		{
			return;
		}
		Map map = selectedPawn.Map;
		IntVec3 clickCell = IntVec3.FromVector3(clickPos);
		HashSet<Pawn> processedPawns = new HashSet<Pawn>();
		for (int dx = -1; dx <= 1; dx++)
		{
			for (int dz = -1; dz <= 1; dz++)
			{
				IntVec3 curCell = clickCell + new IntVec3(dx, 0, dz);
				if (!curCell.InBounds(map))
				{
					continue;
				}
				List<Thing> thingList = map.thingGrid.ThingsListAt(curCell);
				for (int i = 0; i < thingList.Count; i++)
				{
					if (thingList[i] is Pawn hitPawn && processedPawns.Add(hitPawn) && TryResolveForHitPawn(selectedPawn, hitPawn, out var initiator, out var target))
					{
						AddTalkOption(result, initiator, target);
					}
				}
			}
		}
	}

	private static bool TryResolveForHitPawn(Pawn selectedPawn, Pawn hitPawn, out Pawn initiator, out Pawn target)
	{
		initiator = null;
		target = null;
		if (selectedPawn == null || hitPawn == null)
		{
			return false;
		}
		if (hitPawn == selectedPawn)
		{
			if (Settings.Get().PlayerDialogueMode == Settings.PlayerDialogueMode.Disabled)
			{
				return false;
			}
			Pawn playerPawn = global::RimTalk.Data.Cache.GetPlayer();
			if (playerPawn == null)
			{
				return false;
			}
			initiator = playerPawn;
			target = selectedPawn;
			return true;
		}
		if (!IsValidPawnToPawnConversation(selectedPawn, hitPawn))
		{
			return false;
		}
		initiator = selectedPawn;
		target = hitPawn;
		return true;
	}

	private static bool IsValidPawnToPawnConversation(Pawn initiator, Pawn target)
	{
		if (initiator == null || target == null)
		{
			return false;
		}
		if (!initiator.Spawned || initiator.Dead)
		{
			return false;
		}
		if (!target.Spawned || target.Dead)
		{
			return false;
		}
		if (!initiator.IsTalkEligible())
		{
			return false;
		}
		RaceProperties raceProps = target.RaceProps;
		if ((raceProps == null || !raceProps.Humanlike) && !target.HasVocalLink())
		{
			return false;
		}
		if (initiator == global::RimTalk.Data.Cache.GetPlayer())
		{
			return true;
		}
		if (!initiator.CanReach(target, PathEndMode.Touch, Danger.None))
		{
			return false;
		}
		return true;
	}

	private static void AddTalkOption(List<FloatMenuOption> result, Pawn initiator, Pawn target)
	{
		if (initiator != null && target != null)
		{
			result.Add(new FloatMenuOption("RimTalk.FloatMenu.ChatWith".Translate(target.LabelShortCap), delegate
			{
				Find.WindowStack.Add(new CustomDialogueWindow(initiator, target));
			}, MenuOptionPriority.Default, null, target));
		}
	}
}
