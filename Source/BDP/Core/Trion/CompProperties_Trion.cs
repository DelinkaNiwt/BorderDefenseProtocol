using Verse;

namespace BDP.Core.Trion
{
    /// <summary>
    /// CompTrion 的 XML 配置。
    /// 第一版只保留资源闭环真正需要的最小配置。
    /// </summary>
    public sealed class CompProperties_Trion : CompProperties
    {
        /// <summary>
        /// 非 Pawn 宿主的基础最大容量。
        /// </summary>
        public float baseMax = 0f;

        /// <summary>
        /// 初始资源百分比，1 表示满值开始。
        /// </summary>
        public float startPercent = 1f;

        /// <summary>
        /// 每天的基础恢复量。第一版先只保留字段，不急着做复杂恢复。
        /// </summary>
        public float recoveryPerDay = 0f;

        /// <summary>
        /// 聚合消耗结算间隔。
        /// </summary>
        public int drainSettleInterval = 60;

        /// <summary>
        /// 恢复结算间隔。
        /// </summary>
        public int recoveryInterval = 150;

        /// <summary>
        /// 构造 Trion 配置并绑定正式 Comp 类型。
        /// </summary>
        public CompProperties_Trion()
        {
            compClass = typeof(CompTrion);
        }
    }
}
