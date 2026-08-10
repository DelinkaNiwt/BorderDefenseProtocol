using BDP.Core.CombatBody;
using BDP.Core.CombatBody.Wounds;
using HarmonyLib;
using Verse;

namespace BDP.Patches
{
    /// <summary>
    /// 战斗体伤口新增事件接线。
    /// 在伤口运行时适用阶段把原版伤口变化转发给 CombatBody 伤口运行层，不改写伤口事实。
    /// </summary>
    [HarmonyPatch(typeof(HediffSet), nameof(HediffSet.AddDirect))]
    public static class Patch_HediffSet_AddDirect_CombatBodyWounds
    {
        /// <summary>
        /// 原版伤口加入后通知战斗体伤口运行层。
        /// </summary>
        public static void Postfix(Hediff hediff)
        {
            if (hediff == null || !CombatBodyWoundPolicy.IsSupportedWound(hediff))
            {
                return;
            }

            Pawn pawn = hediff.pawn;
            if (!CombatBodyWoundPolicy.IsCombatBodyWoundRuntimeApplicable(pawn))
            {
                return;
            }

            pawn.GetComp<CompCombatBodyHost>()?.WoundRuntime.NotifyWoundAddedOrChanged(pawn, hediff);
        }
    }
}
