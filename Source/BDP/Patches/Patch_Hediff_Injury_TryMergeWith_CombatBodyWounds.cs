using BDP.Core.CombatBody;
using BDP.Core.CombatBody.Wounds;
using HarmonyLib;
using Verse;

namespace BDP.Patches
{
    /// <summary>
    /// 战斗体伤口合并事件接线。
    /// 原版伤口合并成功后，旧伤口严重度已经变化，需要刷新派生运行时。
    /// </summary>
    [HarmonyPatch(typeof(Hediff_Injury), nameof(Hediff_Injury.TryMergeWith))]
    public static class Patch_Hediff_Injury_TryMergeWith_CombatBodyWounds
    {
        /// <summary>
        /// 伤口合并成功后通知战斗体伤口运行层。
        /// </summary>
        public static void Postfix(Hediff_Injury __instance, bool __result)
        {
            if (!__result || __instance == null)
            {
                return;
            }

            Pawn pawn = __instance.pawn;
            pawn?.GetComp<CompCombatBodyHost>()?.WoundRuntime.NotifyWoundAddedOrChanged(pawn, __instance);
        }
    }
}
