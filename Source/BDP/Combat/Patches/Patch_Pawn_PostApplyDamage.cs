using BDP.Core;
using HarmonyLib;
using Verse;

namespace BDP.Combat
{
    /// <summary>
    /// Postfix patch on Pawn.PostApplyDamage（v13.1重构：事件解耦）
    ///
    /// 用途: 伤害应用后发布事件通知战斗体系统
    /// - 发布OnDamageReceived事件（由HediffComp_TrionDamageCost订阅）
    ///
    /// 重构说明:
    /// - 移除硬编码的HediffComp方法调用
    /// - 改为发布事件，由订阅者自行处理
    /// - 破裂检测由Hediff_CombatBodyActive轮询完成
    /// - 手部完整性检查由CompTriggerBody订阅事件处理
    ///
    /// 时序说明:
    /// - PostApplyDamage在原版伤害流程之后执行
    /// - 此时MissingPart已由HediffSet.AddDirect自动创建
    /// - 因此可以直接检测Pawn身上的Hediff_MissingPart
    /// </summary>
    [HarmonyPatch(typeof(Pawn), "PostApplyDamage")]
    public static class Patch_Pawn_PostApplyDamage
    {
        public static void Postfix(Pawn __instance, DamageInfo dinfo, float totalDamageDealt)
        {
            // 基础检查
            if (__instance?.health?.hediffSet == null) return;
            if (totalDamageDealt <= 0) return;

            // 检查是否有战斗体激活Hediff
            var hediff = __instance.health.hediffSet.GetFirstHediffOfDef(BDP_DefOf.BDP_CombatBodyActive);
            if (hediff == null) return;

            // 发布伤害接收事件（解耦）
            var args = new DamageReceivedEventArgs
            {
                Pawn = __instance,
                TotalDamageDealt = totalDamageDealt
            };
            BDPEvents.TriggerDamageReceived(args);
        }
    }
}
