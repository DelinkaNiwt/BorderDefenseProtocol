using System;
using System.Reflection;
using BDP.Core.Trigger;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BDP.Patches
{
    /// <summary>
    /// 把无激活 Trion 腺体不得装备触发体接入原版装备资格判定。
    /// 资格服务只识别带有 CompTriggerBody 的 BDP 物品，其他装备保持原版规则。
    /// </summary>
    [HarmonyPatch]
    public static class Patch_EquipmentUtility_CanEquip_TriggerBodyGlandRequirement
    {
        /// <summary>
        /// 解析带 ref reason 与 checkBonded 参数的原版 CanEquip 重载。
        /// </summary>
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(
                typeof(EquipmentUtility),
                nameof(EquipmentUtility.CanEquip),
                new[] { typeof(Thing), typeof(Pawn), typeof(string).MakeByRefType(), typeof(bool) });
        }

        /// <summary>
        /// 在原版判定成功后追加 BDP 触发体腺体资格限制。
        /// </summary>
        public static void Postfix(
            Thing thing,
            Pawn pawn,
            ref string cantReason,
            ref bool __result)
        {
            if (!__result || TriggerBodyEquipmentEligibility.CanEquip(thing, pawn))
            {
                return;
            }

            __result = false;
            cantReason = "BDP_Message_Trigger_RequiresTrionGland".Translate().ToString();
        }
    }
}
