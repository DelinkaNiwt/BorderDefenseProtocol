using BDP.Core.BodyConstraints;
using HarmonyLib;
using Verse;

namespace BDP.Patches
{
    /// <summary>
    /// 身体约束信号接线。
    /// 只在缺失部位正式加入后发布中性信号，不直接触碰下游业务。
    /// </summary>
    [HarmonyPatch(typeof(HediffSet), nameof(HediffSet.AddDirect))]
    /// <summary>
    /// 缺失部位加入时的身体约束信号补丁。
    /// </summary>
    public static class Patch_HediffSet_AddDirect_BodyConstraintSignal
    {
        /// <summary>
        /// 缺失部位加入后发布中性身体约束变化。
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
