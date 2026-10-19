using System;
using BDP.Core.Abilities;
using BDP.Core.AttackExecution;
using BDP.Core.Verbs;
using HarmonyLib;
using Verse;

namespace BDP.Patches
{
    /// <summary>只补齐外部动作的可用性；自有宿主直接查询中性入口。</summary>
    [HarmonyPatch(typeof(Verb), nameof(Verb.Available))]
    public static class Patch_Verb_Available_AttackAdmission
    {
        /// <summary>不覆盖原版已拒绝的结果，也不重复查询已接入的自有可用性。</summary>
        public static void Postfix(Verb __instance, ref bool __result)
        {
            if (!__result || __instance is BdpVerb_Shoot || __instance is BdpVerb_CastAbility) return;
            __result = AttackAdmissionAccess.Evaluate(__instance?.CasterPawn, __instance).Accepted;
        }
    }

    /// <summary>外部直接起手适配，不承担具体业务限制。</summary>
    [HarmonyPatch(typeof(Verb), nameof(Verb.TryStartCastOn), new Type[] {
        typeof(LocalTargetInfo), typeof(LocalTargetInfo), typeof(bool), typeof(bool), typeof(bool), typeof(bool) })]
    public static class Patch_Verb_TryStartCastOn_AttackAdmission
    {
        /// <summary>自有射击及普通施放已在准备前查询；自身施放单独覆写，不能粗略跳过所有 BDP 类型。</summary>
        public static bool Prefix(Verb __instance, ref bool __result)
        {
            if (__instance is BdpVerb_Shoot
                || (__instance is BdpVerb_CastAbility && !(__instance is BdpVerb_CastSelfAbility))) return true;
            if (AttackAdmissionAccess.Evaluate(__instance?.CasterPawn, __instance).Accepted) return true;
            __result = false;
            return false;
        }
    }

    /// <summary>原版连射中断适配：动作不可用后的原版收尾不等同于直接重置。</summary>
    [HarmonyPatch(typeof(Verb), "TryCastNextBurstShot")]
    public static class Patch_Verb_TryCastNextBurstShot_AttackAdmission
    {
        /// <summary>受限时沿用既有重置，防止额外完成回调、冷却和整轮燃料扣除。</summary>
        public static bool Prefix(Verb __instance)
        {
            if (AttackAdmissionAccess.Evaluate(__instance?.CasterPawn, __instance).Accepted) return true;
            __instance.Reset();
            return false;
        }
    }
}
