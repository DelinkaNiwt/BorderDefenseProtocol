using HarmonyLib;
using RimWorld;
using Verse;

namespace VanillaPsycastsExpanded;

[HarmonyPatch(typeof(Pawn_SkillTracker), "Learn")]
public class Pawn_SkillTracker_Learn_Patch
{
	private static bool Prefix(SkillDef sDef, float xp, Pawn ___pawn)
	{
		if (___pawn.story?.traits?.HasTrait(VPE_DefOf.VPE_Thrall) == true && xp > 0f)
		{
			return false;
		}
		return true;
	}
}
