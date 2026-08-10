using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BDP.Core.Trion.Capacity
{
    /// <summary>
    /// 单个 Trion 潜在容量生成档位。
    /// </summary>
    public sealed class TrionCapacityPotentialGenerationBand
    {
        /// <summary>档位包含的最低容量。</summary>
        public int minimumCapacity;

        /// <summary>档位包含的最高容量。</summary>
        public int maximumCapacity;

        /// <summary>档位参与随机选择的相对权重。</summary>
        public float weight;
    }

    /// <summary>
    /// Trion 潜在容量的可配置分层分布。
    /// </summary>
    public sealed class TrionCapacityPotentialDistributionDef : Def
    {
        /// <summary>全局最低容量。</summary>
        public int minimumCapacity = 100;

        /// <summary>全局最高容量。</summary>
        public int maximumCapacity = 5000;

        /// <summary>数值量化单位。</summary>
        public int quantizationUnit = 100;

        /// <summary>按相对权重选择的容量档位。</summary>
        public List<TrionCapacityPotentialGenerationBand> bands = new List<TrionCapacityPotentialGenerationBand>();
    }

    /// <summary>
    /// Trion 潜在容量分布定义引用。
    /// </summary>
    [DefOf]
    public static class TrionCapacityPotentialDistributionDefOf
    {
        /// <summary>正式潜在容量分布。</summary>
        public static TrionCapacityPotentialDistributionDef BDP_TrionCapacityPotentialDistribution;

        /// <summary>确保定义引用完成初始化。</summary>
        static TrionCapacityPotentialDistributionDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(TrionCapacityPotentialDistributionDefOf));
        }
    }
}
