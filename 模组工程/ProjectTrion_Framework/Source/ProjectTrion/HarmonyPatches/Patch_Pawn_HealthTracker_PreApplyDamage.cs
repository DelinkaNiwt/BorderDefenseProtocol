using HarmonyLib;
using ProjectTrion.Components;
using Verse;

namespace ProjectTrion.HarmonyPatches
{
    /// <summary>
    /// Harmony补丁：拦截伤害并转化为Trion消耗。
    ///
    /// Harmony patch: Intercept damage and convert to Trion consumption.
    /// 当单位处于战斗体状态时，伤害不影响肉身，全部转化为Trion消耗。
    /// </summary>
    [HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.PreApplyDamage))]
    [HarmonyPriority(Priority.High)]  // 高优先级，确保在其他mod补丁之前执行
    public static class Patch_Pawn_HealthTracker_PreApplyDamage
    {
        /// <summary>
        /// 前置补丁：在应用伤害前拦截。
        /// Prefix patch: Intercept before damage is applied.
        /// </summary>
        public static void Prefix(Pawn ___pawn, ref DamageInfo dinfo)
        {
            if (___pawn == null)
                return;

            // 获取Trion组件
            var compTrion = ___pawn.GetComp<CompTrion>();
            if (compTrion == null || !compTrion.IsInCombat)
                return;

            // 虚拟伤害系统：将伤害转化为Trion消耗
            float damageAmount = dinfo.Amount;

            // 调用Strategy的伤害转化
            var strategy = compTrion.Strategy;
            if (strategy != null)
            {
                // Strategy可以自定义伤害转化比率
                // （框架使用默认1:1转化）
                compTrion.Consume(damageAmount);
            }

            // 清空伤害值，防止肉身受伤
            dinfo.SetAmount(0);

            // 调试日志
            #if DEBUG
            Log.Message($"ProjectTrion: {___pawn.Name}在战斗体状态下拦截伤害{damageAmount}，转化为Trion消耗");
            #endif
        }
    }
}
