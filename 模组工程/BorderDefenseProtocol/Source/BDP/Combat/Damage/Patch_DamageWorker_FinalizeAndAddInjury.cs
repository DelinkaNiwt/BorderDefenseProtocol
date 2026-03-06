using HarmonyLib;
using System;
using RimWorld;
using Verse;

namespace BDP.Combat
{
    /// <summary>
    /// Prefix patch DamageWorker_AddInjury.FinalizeAndAddInjury 重载2。
    ///
    /// 拦截点：原版已完成部位选择、护甲减伤、伤口类型确定、Hediff_Injury构造。
    /// - injury.Severity = 护甲后伤害
    /// - injury.Part = 命中部位
    /// - injury.def = 伤口类型
    ///
    /// 返回 false = 跳过原版 AddHediff（伤害被战斗体吸收）。
    ///
    /// 架构说明：
    /// - 迁移自 PreApplyDamage 拦截点
    /// - 新拦截点时机更晚，可直接读取原版计算结果
    /// - 避免手动猜测部位和模拟护甲计算
    /// </summary>
    [HarmonyPatch(typeof(DamageWorker_AddInjury), "FinalizeAndAddInjury",
        new Type[] { typeof(Pawn), typeof(Hediff_Injury), typeof(DamageInfo), typeof(DamageWorker.DamageResult) })]
    public static class Patch_DamageWorker_FinalizeAndAddInjury
    {
        public static bool Prefix(Pawn pawn, Hediff_Injury injury, DamageInfo dinfo, DamageWorker.DamageResult result)
        {
            // 基础检查
            if (pawn == null || injury == null) return true;

            // 检查是否有战斗体运行时且已激活
            var runtime = CombatBodyRuntime.Of(pawn);
            if (runtime == null || !runtime.IsActive) return true;

            // 调用战斗体伤害处理系统
            CombatBodyDamageHandler.HandleDamage(pawn, injury, dinfo);

            // 返回 false 跳过原版 pawn.health.AddHediff
            return false;
        }
    }
}
