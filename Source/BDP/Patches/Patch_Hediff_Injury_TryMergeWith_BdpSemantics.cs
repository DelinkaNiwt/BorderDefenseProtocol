using BDP.Core.Semantics;
using HarmonyLib;
using Verse;

namespace BDP.Patches
{
    /// <summary>
    /// 伤口合并时的最小接线。
    /// 原版合并只会累加严重度，不会刷新旧伤口的来源字段。
    /// 这里在“当前确实存在 BDP 攻击语义且本次合并成功”时，补一次来源字段刷新。
    /// </summary>
    [HarmonyPatch(typeof(Hediff_Injury), nameof(Hediff_Injury.TryMergeWith))]
    /// <summary>
    /// 伤口合并后的来源刷新补丁。
    /// </summary>
    public static class Patch_Hediff_Injury_TryMergeWith_BdpSemantics
    {
        /// <summary>
        /// 把合并后的旧伤口来源改成当前攻击行为来源，
        /// 避免新一枪虽然语义正确，但因为并入旧伤口而继续显示旧来源。
        /// </summary>
        public static void Postfix(Hediff_Injury __instance, Hediff other, bool __result)
        {
            if (!__result)
            {
                return;
            }

            ISemanticContext semanticContext = SemanticRuntimeScope.Current;
            if (semanticContext == null)
            {
                return;
            }

            // 这里沿用旧伤口现有来源细节，只刷新需要显示给玩家看的来源语义。
            BdpDamageSemanticBridge.TryApplyInjurySource(
                __instance,
                semanticContext,
                __instance.sourceDef,
                __instance.sourceToolLabel,
                __instance.sourceBodyPartGroup);
        }
    }
}
