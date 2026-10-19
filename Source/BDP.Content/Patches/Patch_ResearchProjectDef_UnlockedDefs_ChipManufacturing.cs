using System.Collections.Generic;
using BDP.Content.Assembly.ChipManufacturing.Defs;
using BDP.Content.Research;
using HarmonyLib;
using Verse;

namespace BDP.Content.Patches
{
    /// <summary>仅补足BDP研究的自定义芯片条目，不替换原版研究界面与共同解锁分组。</summary>
    [HarmonyPatch(typeof(ResearchProjectDef), nameof(ResearchProjectDef.UnlockedDefs), MethodType.Getter)]
    public static class Patch_ResearchProjectDef_UnlockedDefs_ChipManufacturing
    {
        /// <summary>保留原版产物，追加制造定义中的功能与显式构型。</summary>
        public static void Postfix(ResearchProjectDef __instance, ref List<Def> __result)
        {
            if (__instance?.tab?.defName != "BDP_ResearchTab")
            {
                return;
            }
            __result = ResearchUnlockDisplay.Build(__instance, __result,
                DefDatabase<ChipManufacturingPresetDef>.AllDefsListForReading,
                DefDatabase<ChipArmamentFormDef>.AllDefsListForReading);
        }
    }
}
