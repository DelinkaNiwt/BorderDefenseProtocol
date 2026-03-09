using System.Collections.Generic;
using BDP.Core;
using HarmonyLib;
using Verse;

namespace BDP.Combat.Patches
{
    /// <summary>
    /// Postfix patch on HealthUtility.GetHediffDefFromDamage
    ///
    /// 战斗体激活时，将原版伤口HediffDef替换为BDP自定义版本。
    /// BDP版本: painPerSeverity=0, partIgnoreMissingHP=true, 无感染/永久化。
    /// </summary>
    [HarmonyPatch(typeof(HealthUtility), nameof(HealthUtility.GetHediffDefFromDamage))]
    public static class Patch_HealthUtility_GetHediffDefFromDamage
    {
        /// <summary>
        /// 原版defName → BDP defName 映射表。
        /// 启动时通过 DefDatabase 解析，避免硬编码引用。
        /// </summary>
        private static Dictionary<string, string> vanillaToBDP;

        private static void EnsureMapping()
        {
            if (vanillaToBDP != null) return;
            vanillaToBDP = new Dictionary<string, string>
            {
                { "Gunshot",   "BDP_Gunshot" },
                { "Cut",       "BDP_Cut" },
                { "Crush",     "BDP_Crush" },
                { "Crack",     "BDP_Crack" },
                { "Stab",      "BDP_Stab" },
                { "Scratch",   "BDP_Scratch" },
                { "Bite",      "BDP_Bite" },
                { "Burn",      "BDP_Burn" },
                { "Shredded",  "BDP_Shredded" },
                { "Misc",      "BDP_Misc" },
                { "Bruise",    "BDP_Bruise" },
                { "BeamWound", "BDP_BeamWound" },
            };
        }

        /// <summary>
        /// Postfix: 如果Pawn有战斗体激活，替换返回的HediffDef。
        /// </summary>
        static void Postfix(ref HediffDef __result, Pawn pawn)
        {
            // 检查Pawn是否有战斗体激活
            if (!CombatBodyQuery.IsCombatBodyActive(pawn)) return;

            EnsureMapping();

            // 查找对应的BDP版本
            if (vanillaToBDP.TryGetValue(__result.defName, out string bdpDefName))
            {
                var bdpDef = DefDatabase<HediffDef>.GetNamedSilentFail(bdpDefName);
                if (bdpDef != null)
                {
                    __result = bdpDef;
                }
            }
            // 没有对应映射的伤口类型保持原版（兼容其他模组添加的伤害类型）
        }
    }
}
