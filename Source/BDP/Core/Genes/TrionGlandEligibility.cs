using RimWorld;
using Verse;

namespace BDP.Core.Genes
{
    /// <summary>
    /// Trion 腺体基因的唯一有效资格判定。
    /// </summary>
    public static class TrionGlandEligibility
    {
        /// <summary>
        /// 判断角色是否拥有且正在表达正式 Trion 腺体基因。
        /// </summary>
        public static bool HasActiveTrionGland(Pawn pawn)
        {
            Gene gene = pawn?.genes?.GetGene(TrionGeneDefOf.BDP_Gene_TrionGland);
            return gene != null && gene.Active;
        }
    }

    /// <summary>
    /// Trion 正式基因定义引用。
    /// </summary>
    [DefOf]
    public static class TrionGeneDefOf
    {
        /// <summary>正式 Trion 腺体基因。</summary>
        public static GeneDef BDP_Gene_TrionGland;

        /// <summary>确保定义引用完成初始化。</summary>
        static TrionGeneDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(TrionGeneDefOf));
        }
    }
}
