using HarmonyLib;
using RimWorld;
using Verse;

namespace Milira;

[HarmonyPatch(typeof(Faction))]
[HarmonyPatch("Notify_MemberExitedMap")]
public static class Milira_FactionMemberRelease_Patch
{
	[HarmonyPostfix]
	public static void Postfix(Pawn member, bool freed)
	{
		Faction faction = Find.FactionManager.FirstFactionOfDef(MiliraDefOf.Milira_Faction);
		if (member.InMentalState || !(member.health.hediffSet.BleedRateTotal < 0.001f))
		{
			return;
		}
		MiliraGameComponent_OverallControl component = Current.Game.GetComponent<MiliraGameComponent_OverallControl>();
		if (member == component.pawn && member.Faction == faction && freed && !faction.def.permanentEnemyToEveryoneExcept.Contains(Faction.OfPlayer.def))
		{
			float num = component.miliraThreatPoint;
			if (member.def.defName == "Milira_Race" && num < 20f)
			{
				Log.Message("memberExit");
				component.turnToFriend_Pre = false;
			}
		}
	}
}
