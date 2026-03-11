using HarmonyLib;
using Verse;
using BDP.Core;

namespace BDP.Combat
{
    /// <summary>
    /// 拦截触发体掉落。
    ///
    /// 原版行为：双手缺失 → Manipulation=0 → CheckForStateChange → TryDropEquipment
    /// 问题：战斗体下手臂影子HP耗尽 → 真身 MissingPart → 原版强制掉落触发体
    /// 修复：战斗体激活时，阻止触发体被掉落（普通武器不受影响）
    ///
    /// 判断依据：
    /// - 被掉落的装备实现了 ICombatBodySupport 接口（即触发体）
    /// - 装备者的战斗体处于激活状态
    /// </summary>
    [HarmonyPatch(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.TryDropEquipment))]
    public static class Patch_EquipmentTracker_TryDropEquipment
    {
        static bool Prefix(
            Pawn_EquipmentTracker __instance,
            ThingWithComps eq,
            ref ThingWithComps resultingEq,
            ref bool __result)
        {
            if (eq == null) return true;

            // 检查被掉落的装备是否为触发体（通过 ICombatBodySupport 接口判断，避免跨模块引用）
            bool isTriggerBody = CombatBodyQuery.FindCombatBodySupport(eq) != null;
            if (!isTriggerBody) return true;

            // 检查装备者是否战斗体激活中或延时破裂中
            var pawn = __instance.pawn;
            var runtime = CombatBodyRuntime.Of(pawn);
            if (runtime == null || (!runtime.IsActive && !runtime.State.IsCollapsing))
                return true; // 战斗体未激活且未破裂，正常掉落

            // 战斗体激活中或延时破裂中：阻止触发体掉落
            string state = runtime.IsActive ? "激活中" : "延时破裂中";
            Log.Message($"[BDP] 拦截触发体掉落: {eq.LabelShortCap} (战斗体{state}，Manipulation丧失不影响装备持有)");
            resultingEq = null;
            __result = false;
            return false;
        }
    }
}
