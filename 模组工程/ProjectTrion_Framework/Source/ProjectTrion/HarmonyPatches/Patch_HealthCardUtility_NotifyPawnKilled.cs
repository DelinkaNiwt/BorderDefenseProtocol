using HarmonyLib;
using ProjectTrion.Components;
using ProjectTrion.Core;
using Verse;

namespace ProjectTrion.HarmonyPatches
{
    /// <summary>
    /// Harmony补丁：检测Pawn死亡事件，强制解除战斗体。
    ///
    /// Harmony patch: Detect pawn death, force deactivate combat body.
    /// 当单位在战斗体状态下被击杀时，强制解除战斗体以保护宿主生命。
    /// </summary>
    [HarmonyPatch(typeof(HealthCardUtility), nameof(HealthCardUtility.Notify_PawnKilled))]
    [HarmonyPriority(Priority.High)]
    public static class Patch_HealthCardUtility_NotifyPawnKilled
    {
        /// <summary>
        /// 前置补丁：在Pawn死亡前拦截。
        /// Prefix patch: Intercept before pawn is killed.
        /// </summary>
        public static void Prefix(Pawn pawn)
        {
            if (pawn == null)
                return;

            // 获取Trion组件
            var compTrion = pawn.GetComp<CompTrion>();
            if (compTrion == null || !compTrion.IsInCombat)
                return;

            #if DEBUG
            Log.Message($"ProjectTrion: {pawn.Name}在战斗体状态下被击杀，强制解除战斗体");
            #endif

            // 强制解除战斗体
            compTrion.DestroyCombatBody(DestroyReason.Other);

            // 注意：不能阻止死亡事件继续进行
            // 战斗体摧毁后，宿主肉身会根据快照状态被恢复
            // 如果宿身体状态确实致命，仍然会死亡（这是正确的）
        }
    }
}
