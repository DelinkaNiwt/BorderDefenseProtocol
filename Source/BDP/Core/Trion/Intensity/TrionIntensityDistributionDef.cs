using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BDP.Core.Trion.Intensity
{
    /// <summary>
    /// 一项可生成的先天 Trion 释放力及其相对权重。
    /// </summary>
    public sealed class TrionIntensityWeight
    {
        /// <summary>先天 Trion 释放力整数。</summary>
        public int intensity;

        /// <summary>参与随机选择的相对权重。</summary>
        public float weight;
    }

    /// <summary>
    /// 先天 Trion 释放力的独立生成分布。
    /// </summary>
    public sealed class TrionIntensityDistributionDef : Def
    {
        /// <summary>按定义顺序参与随机选择的释放力条目。</summary>
        public List<TrionIntensityWeight> values = new List<TrionIntensityWeight>();
    }

    /// <summary>
    /// Trion 释放力分布定义引用。
    /// </summary>
    [DefOf]
    public static class TrionIntensityDistributionDefOf
    {
        /// <summary>正式先天释放力分布。</summary>
        public static TrionIntensityDistributionDef BDP_TrionIntensityDistribution;

        /// <summary>确保定义引用完成初始化。</summary>
        static TrionIntensityDistributionDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(TrionIntensityDistributionDefOf));
        }
    }
}
