using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace TurbojetBackpack;

[HarmonyPatch(typeof(Log), "WarningOnce")]
public static class Patch_SuppressPathWarning
{
	public static bool Prefix(string text, int key)
	{
		if (TurbojetGlobal.IsCustomPathfinding && text.Contains("Resolved path returned no nodes"))
		{
			return false;
		}
		if (text.StartsWith("Resolved path returned no nodes"))
		{
			int num = text.LastIndexOf("for ");
			if (num > 0)
			{
				string text2 = text.Substring(num + 4).TrimEnd(')');
				List<Map> maps = Find.Maps;
				for (int i = 0; i < maps.Count; i++)
				{
					IReadOnlyList<Pawn> allPawnsSpawned = maps[i].mapPawns.AllPawnsSpawned;
					for (int j = 0; j < allPawnsSpawned.Count; j++)
					{
						Pawn pawn = allPawnsSpawned[j];
						if (pawn.Name != null && (pawn.Name.ToStringShort == text2 || pawn.Name.ToStringFull == text2 || pawn.LabelShort == text2) && TurbojetGlobal.IsFlightActive(pawn))
						{
							return false;
						}
					}
				}
			}
		}
		return true;
	}
}
