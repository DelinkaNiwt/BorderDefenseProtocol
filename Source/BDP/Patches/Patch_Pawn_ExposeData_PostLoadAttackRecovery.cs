using BDP.Core.AttackExecution;
using HarmonyLib;
using Verse;

namespace BDP.Patches
{
    /// <summary>
    /// Pawn 读档后收口或续接 BDP 攻击会话的补丁。
    /// PostLoadInit 只做会话真值校验与版本重绑，不在这里重建新的攻击计划。
    /// </summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.ExposeData))]
    public static class Patch_Pawn_ExposeData_PostLoadAttackRecovery
    {
        /// <summary>
        /// 在 Pawn 整体读档完成后，主动校验并收口或续接旧的 BDP 攻击会话。
        /// </summary>
        public static void Postfix(Pawn __instance)
        {
            if (Scribe.mode == LoadSaveMode.PostLoadInit && __instance != null)
            {
                AttackExecutionPostLoadRecovery.RecoverStaleAttackSession(__instance);
            }
        }
    }
}
