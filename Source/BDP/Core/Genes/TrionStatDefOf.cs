using RimWorld;
using Verse;

namespace BDP.Core.Genes
{
    /// <summary>
    /// Trion 基因系统使用的 StatDef 引用。
    /// </summary>
    [DefOf]
    public static class TrionStatDefOf
    {
        /// <summary>
        /// Trion 容量。
        /// </summary>
        public static StatDef BDP_TrionCapacity;

        /// <summary>
        /// Trion 每日恢复量。
        /// </summary>
        public static StatDef BDP_TrionRecoveryRate;

        /// <summary>
        /// Trion 释放力。
        /// </summary>
        public static StatDef BDP_TrionIntensity;

        /// <summary>
        /// 确保 DefOf 在静态构造阶段完成初始化。
        /// </summary>
        static TrionStatDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(TrionStatDefOf));
        }
    }
}
