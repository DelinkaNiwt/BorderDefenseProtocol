using HarmonyLib;
using RimWorld;
using Verse;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>只给 BDP 临时特性追加来源说明。</summary>
    [HarmonyPatch(typeof(Trait), nameof(Trait.TipString))]
    public static class Patch_Trait_TipString_BlackTriggerSource
    {
        /// <summary>保留原版提示并追加本地化说明。</summary>
        public static void Postfix(Trait __instance, ref string __result)
        {
            if (!(__instance is Trait_BlackTriggerInherited)) return;
            __result += "\n\n" + "BDP_BlackTrigger_Inheritance_TemporarySource".Translate();
        }
    }
}
