using System;
using Verse;

namespace BDP.Core.CombatBody.Wounds
{
    /// <summary>
    /// 战斗体伤口原版指标读取口。
    /// 它用于读取 BDP 压制前的原版伤口数据，避免 Trion 计算读到被压制后的结果。
    /// </summary>
    internal static class CombatBodyWoundRawMetrics
    {
        /// <summary>
        /// 当前线程是否正在绕过战斗体伤口流血压制。
        /// </summary>
        [ThreadStatic]
        private static bool isBypassingBleedSuppression;

        /// <summary>
        /// 当前线程是否正在读取原版 rawBleedRate。
        /// </summary>
        internal static bool IsBypassingBleedSuppression
        {
            get { return isBypassingBleedSuppression; }
        }

        /// <summary>
        /// 读取原版伤口流血潜势。
        /// 返回值会经过非负保护，但不会应用 BDP 的 Active 压制。
        /// </summary>
        internal static float ReadRawBleedRate(Hediff hediff)
        {
            if (hediff == null || !CombatBodyWoundPolicy.IsSupportedWound(hediff))
            {
                return 0f;
            }

            bool previousBypassState = isBypassingBleedSuppression;
            isBypassingBleedSuppression = true;
            try
            {
                return Math.Max(0f, hediff.BleedRate);
            }
            finally
            {
                isBypassingBleedSuppression = previousBypassState;
            }
        }
    }
}
