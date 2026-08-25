using BDP.Core.AttackExecution;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BDP.Patches
{
    /// <summary>
    /// 补足原版近战未命中或被闪避时的“攻击动作已执行”通知。
    /// 命中时由统一 burst 入口发布，避免重复广播。
    /// </summary>
    [HarmonyPatch(typeof(Verb_MeleeAttack), "TryCastShot")]
    public static class Patch_Verb_MeleeAttack_TryCastShot_BdpAttackActionSuccess
    {
        /// <summary>
        /// 记录本次近战是否已经进入有效攻击动作。
        /// </summary>
        public static void Prefix(Verb_MeleeAttack __instance, ref bool __state)
        {
            Pawn pawn = __instance != null ? __instance.CasterPawn : null;
            __state = pawn != null
                && pawn.Spawned
                && pawn.stances != null
                && !pawn.stances.FullBodyBusy
                && __instance.CurrentTarget.IsValid
                && __instance.CurrentTarget.Thing != null;
        }

        /// <summary>
        /// 近战动作已经完成但没有命中时仍关闭隐身芯片。
        /// </summary>
        public static void Postfix(Verb_MeleeAttack __instance, bool __result, bool __state)
        {
            if (!__state || __result)
            {
                return;
            }

            AttackActionSuccessDispatcher.Publish(
                AttackActionSuccess.FromWeapon(__instance, false));
        }
    }
}
