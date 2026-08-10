using BDP.Core.BodyConstraints;
using HarmonyLib;
using Verse;

namespace BDP.Patches
{
    /// <summary>
    /// 身体约束信号接线。
    /// 只在缺失部位正式移除后发布中性信号，不直接触碰下游业务。
    /// </summary>
    [HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.RemoveHediff))]
    /// <summary>
    /// 缺失部位移除时的身体约束信号补丁。
    /// </summary>
    public static class Patch_Pawn_HealthTracker_RemoveHediff_BodyConstraintSignal
    {
        /// <summary>
        /// 缺失部位移除后发布中性身体约束变化。
        /// </summary>
        public static void Postfix(Hediff hediff)
        {
            if (!(hediff is Hediff_MissingPart))
            {
                return;
            }

            PawnBodyConstraintSignalHub.Publish(hediff.pawn, PawnBodyConstraintChangeKind.MissingPartChanged);
        }
    }
}
