using UnityEngine;
using Verse;

namespace BDP.Content.Hediffs
{
    /// <summary>
    /// 提供 Hediff（健康状态）标签的通用显示辅助。
    /// </summary>
    internal static class HediffLabelUtility
    {
        /// <summary>
        /// 在基础标签后追加当前严重度百分比。
        /// </summary>
        public static string AppendSeverityPercent(
            string labelBase,
            float severity)
        {
            return labelBase + Mathf.Clamp01(severity).ToStringPercent("0");
        }
    }
}
