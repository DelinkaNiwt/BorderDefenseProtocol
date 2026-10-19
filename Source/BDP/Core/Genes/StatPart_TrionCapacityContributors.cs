using BDP.Core.Trion.Capacity;
using RimWorld;
using Verse;

namespace BDP.Core.Genes
{
    /// <summary>
    /// 把外部 Trion 容量贡献提供器投影到 Pawn 的最大容量 StatDef。
    /// </summary>
    public sealed class StatPart_TrionCapacityContributors : StatPart
    {
        /// <summary>
        /// 只对拥有激活 Trion 腺体的 Pawn 加入外部容量贡献。
        /// </summary>
        public override void TransformValue(StatRequest req, ref float val)
        {
            Pawn pawn = req.Thing as Pawn;
            if (!TrionGlandEligibility.HasActiveTrionGland(pawn))
            {
                return;
            }

            float contribution = TrionCapacityContributorRegistry.ResolveAdditionalCapacity(pawn);
            if (contribution > 0f && !float.IsNaN(contribution) && !float.IsInfinity(contribution))
            {
                val += contribution;
            }
        }

        /// <summary>
        /// 不新增容量拆分说明，避免把单一 Trion 状态条拆成多个来源面板。
        /// </summary>
        public override string ExplanationPart(StatRequest req)
        {
            return null;
        }
    }
}
