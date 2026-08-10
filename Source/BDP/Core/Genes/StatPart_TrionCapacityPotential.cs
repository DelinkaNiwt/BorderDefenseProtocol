using BDP.Core.Trion;
using RimWorld;
using Verse;

namespace BDP.Core.Genes
{
    /// <summary>
    /// 有效腺体把永久容量潜质投影为实际 Trion 容量。
    /// </summary>
    public sealed class StatPart_TrionCapacityPotential : StatPart
    {
        /// <summary>
        /// 仅在有效腺体存在时加入潜在容量。
        /// </summary>
        public override void TransformValue(StatRequest req, ref float val)
        {
            Pawn pawn = req.Thing as Pawn;
            if (!TrionGlandEligibility.HasActiveTrionGland(pawn))
            {
                return;
            }

            ITrionReader reader = TrionSurfaceAccess.ResolveReader(pawn);
            val += reader?.TrionCapacityPotential ?? 0;
        }

        /// <summary>
        /// 为原版属性说明页提供动态容量来源。
        /// </summary>
        public override string ExplanationPart(StatRequest req)
        {
            Pawn pawn = req.Thing as Pawn;
            if (!TrionGlandEligibility.HasActiveTrionGland(pawn))
            {
                return null;
            }

            ITrionReader reader = TrionSurfaceAccess.ResolveReader(pawn);
            return reader == null
                ? null
                : "BDP_Stat_TrionCapacityPotential".Translate(
                    reader.TrionCapacityPotential);
        }
    }
}
