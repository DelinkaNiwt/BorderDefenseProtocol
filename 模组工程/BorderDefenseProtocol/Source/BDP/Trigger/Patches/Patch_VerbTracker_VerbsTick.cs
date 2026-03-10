using HarmonyLib;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// Postfix patch on VerbTracker.VerbsTick（v14.0自动攻击支持）。
    ///
    /// 用途：驱动芯片Verb的VerbTick，使burst连射正常工作。
    ///
    /// 原问题：
    /// - 芯片Verb不在VerbTracker.AllVerbs中（v5.1设计，避免被近战选择池拾取）
    /// - VerbTracker.VerbsTick()只tick AllVerbs中的Verb
    /// - 芯片Verb的VerbTick不被引擎调用 → ticksToNextBurstShot不递减 → burst连射第2发起卡住
    ///
    /// 解决方案：
    /// - Postfix拦截VerbTracker.VerbsTick
    /// - 检查directOwner是否为CompTriggerBody
    /// - 调用triggerComp.TickChipVerbs()手动tick芯片Verb
    ///
    /// Double-tick防护：
    /// - VerbTick不是幂等的（ticksToNextBurstShot--）
    /// - JobDriver中可能手动调用VerbTick
    /// - CompTriggerBody.TickChipVerbs()内部用lastChipVerbTickedTick字段防止同一tick内重复tick
    ///
    /// 调用时机：
    /// - Pawn_EquipmentTracker.EquipmentTrackerTick()遍历装备
    /// - 对tickerType != Normal的装备调用GetComp<CompEquippable>().verbTracker.VerbsTick()
    /// - 触发体的CompTriggerBody继承CompEquippable，因此会被调用
    /// </summary>
    [HarmonyPatch(typeof(VerbTracker), "VerbsTick")]
    public static class Patch_VerbTracker_VerbsTick
    {
        public static void Postfix(VerbTracker __instance)
        {
            // 检查directOwner是否为CompTriggerBody
            if (__instance.directOwner is CompTriggerBody triggerComp)
            {
                triggerComp.TickChipVerbs();
            }
        }
    }
}
