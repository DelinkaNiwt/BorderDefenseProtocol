using BDP.Core.Trion;
using BDP.Core.Trion.Intensity;
using RimWorld;
using Verse;

namespace BDP.Core.Genes
{
    /// <summary>
    /// 把角色永久不变的先天 Trion 释放力加入原版属性计算。
    /// </summary>
    public sealed class StatPart_TrionInnateIntensity : StatPart
    {
        /// <summary>加入人形角色的先天释放力底数。</summary>
        public override void TransformValue(StatRequest req, ref float val)
        {
            Pawn pawn = req.Thing as Pawn;
            ITrionReader reader = TrionSurfaceAccess.ResolveReader(pawn);
            val += reader?.InnateTrionIntensity ?? 0;
        }

        /// <summary>在原版属性说明页标明先天底数。</summary>
        public override string ExplanationPart(StatRequest req)
        {
            Pawn pawn = req.Thing as Pawn;
            ITrionReader reader = TrionSurfaceAccess.ResolveReader(pawn);
            return reader == null
                ? null
                : "BDP_Stat_TrionInnateIntensity".Translate(
                    TrionIntensityUtility.FormatLevel(reader.InnateTrionIntensity));
        }
    }
}
