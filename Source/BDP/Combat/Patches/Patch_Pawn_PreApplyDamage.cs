using BDP.Core;
using HarmonyLib;
using Verse;

namespace BDP.Combat
{
    /// <summary>
    /// Postfix patch on Pawn.PreApplyDamage
    ///
    /// 用途: 在战斗体Collapsing期间吸收所有伤害(无敌)
    ///
    /// 重构说明:
    /// - 替代原Prefix全拦截模式
    /// - 只在Collapsing期间生效
    /// - 不影响正常战斗中的原版伤害流程
    /// </summary>
    [HarmonyPatch(typeof(Pawn), "PreApplyDamage")]
    public static class Patch_Pawn_PreApplyDamage
    {
        public static void Postfix(Pawn __instance, ref bool absorbed)
        {
            // 如果伤害已被其他系统吸收,跳过
            if (absorbed) return;

            // 检查是否处于Collapsing状态
            if (__instance.health?.hediffSet == null) return;

            if (__instance.health.hediffSet.HasHediff(BDP_DefOf.BDP_CombatBodyCollapsing))
            {
                absorbed = true; // 吸收所有伤害
            }
        }
    }
}
