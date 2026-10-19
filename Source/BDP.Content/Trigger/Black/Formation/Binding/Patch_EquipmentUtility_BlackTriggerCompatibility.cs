using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>把首次装备适格判断接入原版装备许可。</summary>
    [HarmonyPatch]
    public static class Patch_EquipmentUtility_BlackTriggerCompatibility
    {
        /// <summary>锁定带失败原因输出的 CanEquip 重载。</summary>
        public static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(EquipmentUtility), nameof(EquipmentUtility.CanEquip),
                new[] { typeof(Thing), typeof(Pawn), typeof(string).MakeByRefType(), typeof(bool) });
        }

        /// <summary>不满足适格时拒绝首次装备。</summary>
        public static void Postfix(Thing thing, Pawn pawn, ref string cantReason, ref bool __result)
        {
            if (!__result || thing?.TryGetComp<CompBlackTriggerBiocodable>() == null || pawn == null) return;
            BlackTriggerCompatibilityResult result = BlackTriggerCompatibilityEvaluator.Evaluate(thing, pawn);
            if (!result.Accepted)
            {
                __result = false;
                cantReason = result.ReasonKey.Translate().ToString();
            }
        }
    }
}
