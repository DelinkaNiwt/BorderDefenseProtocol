using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>仅阻止非玩家人物机会性搜索并捡起地面黑触发器。</summary>
    [HarmonyPatch]
    public static class Patch_JobGiver_BlackTriggerPickup
    {
        /// <summary>锁定原版私有武器筛选方法。</summary>
        public static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(JobGiver_PickUpOpportunisticWeapon), "ShouldEquipWeapon");
        }

        /// <summary>显式预装不经过此路径，因此开发者设计的敌方持有者不受影响。</summary>
        public static void Postfix(Thing newWep, Pawn pawn, ref bool __result)
        {
            if (__result && pawn?.Faction != Faction.OfPlayer && newWep?.TryGetComp<CompBlackTriggerBiocodable>() != null) __result = false;
        }
    }
}
