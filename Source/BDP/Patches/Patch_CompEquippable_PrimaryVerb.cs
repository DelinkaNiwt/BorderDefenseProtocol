using BDP.Core.Trigger;
using HarmonyLib;
using Verse;

namespace BDP.Patches
{
    /// <summary>
    /// CompEquippable.PrimaryVerb 桥接补丁。
    /// 当触发体有活跃远程芯片时，把 PrimaryVerb 覆盖为 BDP 的远程 formal host verb，
    /// 使原版 UI 层（UseRangedAttack / GetRangedAttackAction / FloatMenu 等）自动识别为远程武器。
    /// </summary>
    [HarmonyPatch(typeof(CompEquippable), "get_PrimaryVerb")]
    public static class Patch_CompEquippable_PrimaryVerb
    {
        /// <summary>
        /// 在原版 PrimaryVerb 结果之后注入 BDP 远程 formal host verb。
        /// Postfix 不干扰 VerbTracker 的内部缓存，只在返回值层面覆盖。
        /// </summary>
        public static void Postfix(CompEquippable __instance, ref Verb __result)
        {
            if (__instance is CompTriggerBody triggerBody)
            {
                Verb rangedVerb = triggerBody.TryGetActiveRangedPrimaryVerb();
                if (rangedVerb != null)
                    __result = rangedVerb;
            }
        }
    }
}
