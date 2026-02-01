using HarmonyLib;
using Verse;

namespace NCL;

[HarmonyPatch(typeof(MechanitorUtility), "InMechanitorCommandRange")]
internal class Patch_CommanderControlRange
{
	private static void Postfix(Pawn mech, LocalTargetInfo target, ref bool __result)
	{
		if (__result || mech?.def == null || target == null)
		{
			return;
		}
		if (mech.TryGetComp<CompIgnoreCommandRange>() != null)
		{
			__result = true;
			return;
		}
		Map mechMap = mech.Map;
		if (mechMap == null)
		{
			return;
		}
		Pawn overseer = mech.GetOverseer();
		if (overseer?.mechanitor?.OverseenPawns == null)
		{
			return;
		}
		foreach (Pawn commander in overseer.mechanitor.OverseenPawns)
		{
			if (commander != null && commander.Spawned && commander.Map == mechMap && commander.TryGetComp<CompIgnoreCommandRange>() != null)
			{
				float mechToCommander = mech.Position.DistanceTo(commander.Position);
				float targetToCommander = commander.Position.DistanceTo(target.Cell);
				if (mechToCommander <= 1f && targetToCommander <= 1f)
				{
					__result = true;
					break;
				}
			}
		}
	}
}
